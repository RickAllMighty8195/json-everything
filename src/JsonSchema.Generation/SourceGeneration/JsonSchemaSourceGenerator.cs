using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Json.Schema.Generation.Serialization;
using Json.Schema.Generation.SourceGeneration.Emitters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
#pragma warning disable RS1041

namespace Json.Schema.Generation.SourceGeneration;

/// <summary>
/// Source generator that creates static JSON schemas for types decorated with [GenerateJsonSchema].
/// </summary>
[Generator]
public class JsonSchemaSourceGenerator : IIncrementalGenerator
{
	private const string _generateJsonSchemaAttributeName = "Json.Schema.Generation.Serialization.GenerateJsonSchemaAttribute";
	private const string _schemaHandlerAttributeName = "Json.Schema.Generation.SchemaHandlerAttribute";

	/// <summary>
	/// Initializes the incremental generator.
	/// </summary>
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var generationOptions = context.AnalyzerConfigOptionsProvider
			.Select(static (provider, _) =>
			{
				provider.GlobalOptions.TryGetValue("build_property.DisableJsonSchemaSourceGeneration", out var disabled);
				provider.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);
				provider.GlobalOptions.TryGetValue("build_property.JsonSchemaDefaultPropertyNaming", out var namingRaw);
				provider.GlobalOptions.TryGetValue("build_property.JsonSchemaDefaultPropertyOrder", out var orderRaw);

				var defaultNaming = Enum.TryParse<NamingConvention>(namingRaw, ignoreCase: true, out var parsedNaming)
					? parsedNaming
					: NamingConvention.AsDeclared;
				var defaultOrder = Enum.TryParse<PropertyOrder>(orderRaw, ignoreCase: true, out var parsedOrder)
					? parsedOrder
					: PropertyOrder.AsDeclared;

				return new GenerationOptions
				{
					IsDisabled = disabled?.Equals("true", StringComparison.OrdinalIgnoreCase) == true,
					RootNamespace = rootNamespace ?? string.Empty,
					DefaultPropertyNaming = defaultNaming,
					DefaultPropertyOrder = defaultOrder
				};
			});

		var typesWithAttribute = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				_generateJsonSchemaAttributeName,
				static (node, _) => node is TypeDeclarationSyntax,
				static (ctx, _) => GetTypeToGenerate(ctx))
			.Where(static type => type is not null);

		var compilationAndTypesAndOptions = context.CompilationProvider
			.Combine(typesWithAttribute.Collect())
			.Combine(generationOptions);

		context.RegisterSourceOutput(compilationAndTypesAndOptions, static (spc, source) =>
		{
			var generationOptions = source.Right;
			if (generationOptions.IsDisabled) return;

			Execute(source.Left.Left, source.Left.Right, generationOptions, spc);
		});
	}

	private static TypeToGenerate? GetTypeToGenerate(GeneratorAttributeSyntaxContext context)
	{
		if (context.TargetSymbol is not INamedTypeSymbol typeSymbol) return null;

		var attributeData = context.Attributes.FirstOrDefault();
		if (attributeData == null) return null;

		return new TypeToGenerate(typeSymbol, attributeData);
	}

	private static void Execute(Compilation compilation, ImmutableArray<TypeToGenerate?> types, GenerationOptions options, SourceProductionContext context)
	{
		if (types.IsDefaultOrEmpty) return;

		var validTypes = types.Where(t => t != null).Select(t => t!).ToList();
		if (validTypes.Count == 0) return;

		var analyzedTypes = new List<TypeInfo>();
		var allEncounteredTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
		var discoveredTypeOptions = new Dictionary<ITypeSymbol, (NamingConvention Naming, PropertyOrder Order)>(SymbolEqualityComparer.Default);
		foreach (var type in validTypes)
		{
			var typeInfo = TypeAnalyzer.Analyze(compilation, type.TypeSymbol, type.AttributeData, context.ReportDiagnostic, options.DefaultPropertyNaming, options.DefaultPropertyOrder);
			if (typeInfo != null) 
			{
				analyzedTypes.Add(typeInfo);
				RegisterTypeOptions(discoveredTypeOptions, typeInfo.TypeSymbol, typeInfo.PropertyNaming, typeInfo.PropertyOrder);
				CollectAllTypes(compilation, typeInfo, allEncounteredTypes, context.ReportDiagnostic, discoveredTypeOptions, typeInfo.PropertyNaming, typeInfo.PropertyOrder);
			}
		}

		if (analyzedTypes.Count == 0) return;

		var schemaHandlers = DiscoverSchemaHandlers(compilation);

		// Analyze all encountered types that aren't already analyzed
		var allTypeInfos = new List<TypeInfo>(analyzedTypes);
		foreach (var typeSymbol in allEncounteredTypes)
		{
			if (analyzedTypes.Any(t => SymbolEqualityComparer.Default.Equals(t.TypeSymbol, typeSymbol))) continue;

			if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
			{
				var naming = options.DefaultPropertyNaming;
				var order = options.DefaultPropertyOrder;
				if (discoveredTypeOptions.TryGetValue(namedTypeSymbol, out var discovered))
				{
					naming = discovered.Naming;
					order = discovered.Order;
				}

				var typeInfo = TypeAnalyzer.Analyze(compilation, namedTypeSymbol, null, context.ReportDiagnostic, naming, order);
				if (typeInfo != null)
					allTypeInfos.Add(typeInfo);
			}
		}

		ResolvePropertyNameConflicts(allTypeInfos, options.RootNamespace);

		var targetNamespace = options.RootNamespace;
		var classDeclaration = DetectGeneratedJsonSchemasClass(compilation, targetNamespace, context.ReportDiagnostic);
		var generatedCode = SchemaCodeEmitter.EmitGeneratedClass(allTypeInfos, targetNamespace, classDeclaration, schemaHandlers);

		context.AddSource("GeneratedJsonSchemas.g.cs", SourceText.From(generatedCode, Encoding.UTF8));
	}

	private static List<SchemaHandlerInfo> DiscoverSchemaHandlers(Compilation compilation)
	{
		var results = new List<SchemaHandlerInfo>();
		var systemType = compilation.GetTypeByMetadataName("System.Type");
		CollectSchemaHandlers(compilation.Assembly.GlobalNamespace, results, systemType);
		return results;
	}

	private static void CollectSchemaHandlers(INamespaceSymbol namespaceSymbol, List<SchemaHandlerInfo> results, INamedTypeSymbol? systemType)
	{
		foreach (var type in namespaceSymbol.GetTypeMembers())
			CollectSchemaHandlers(type, results, systemType);

		foreach (var nested in namespaceSymbol.GetNamespaceMembers())
			CollectSchemaHandlers(nested, results, systemType);
	}

	private static void CollectSchemaHandlers(INamedTypeSymbol typeSymbol, List<SchemaHandlerInfo> results, INamedTypeSymbol? systemType)
	{
		foreach (var attr in typeSymbol.GetAttributes())
		{
			if (attr.AttributeClass?.ToDisplayString() != _schemaHandlerAttributeName) continue;
			if (attr.ConstructorArguments.Length == 0) continue;
			if (attr.ConstructorArguments[0].Value is not INamedTypeSymbol targetTypeSymbol) continue;

			var applyMethod = typeSymbol.GetMembers("Apply")
				.OfType<IMethodSymbol>()
				.FirstOrDefault(m => m is { IsStatic: true } &&
				                     m.Parameters.Length == 2 &&
				                     m.Parameters[0].Type.Name == "JsonSchemaBuilder" &&
				                     SymbolEqualityComparer.Default.Equals(m.Parameters[1].Type, systemType) &&
				                     (m.ReturnsVoid || m.ReturnType.Name == "JsonSchemaBuilder"));

			if (applyMethod == null) continue;

			var isOpenGenericTarget = targetTypeSymbol.IsUnboundGenericType;

			results.Add(new SchemaHandlerInfo
			{
				HandlerTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				TargetTypeName = targetTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
				IsOpenGenericTarget = isOpenGenericTarget,
				ReturnsBuilder = !applyMethod.ReturnsVoid
			});
		}

		foreach (var nested in typeSymbol.GetTypeMembers())
			CollectSchemaHandlers(nested, results, systemType);
	}

	private static void CollectAllTypes(Compilation compilation, TypeInfo typeInfo, HashSet<ITypeSymbol> allTypes, Action<Diagnostic> reportDiagnostic, Dictionary<ITypeSymbol, (NamingConvention Naming, PropertyOrder Order)> discoveredTypeOptions, NamingConvention naming, PropertyOrder order)
	{
		CollectTypeRecursive(compilation, typeInfo.TypeSymbol, allTypes, reportDiagnostic, discoveredTypeOptions, naming, order);

		foreach (var prop in typeInfo.Properties)
		{
			CollectTypeRecursive(compilation, prop.Type, allTypes, reportDiagnostic, discoveredTypeOptions, naming, order);
		}
	}

	private static void CollectTypeRecursive(Compilation compilation, ITypeSymbol typeSymbol, HashSet<ITypeSymbol> allTypes, Action<Diagnostic> reportDiagnostic, Dictionary<ITypeSymbol, (NamingConvention Naming, PropertyOrder Order)> discoveredTypeOptions, NamingConvention naming, PropertyOrder order)
	{
		var unwrapped = CodeEmitterHelpers.UnwrapNullable(typeSymbol);
		var typeKind = SchemaCodeEmitter.DetermineTypeKind(unwrapped);
		if (typeKind is TypeKind.Boolean or TypeKind.Integer or 
		    TypeKind.Number or TypeKind.String or 
		    TypeKind.DateTime or TypeKind.Guid or 
		    TypeKind.Uri or TypeKind.Enum)
			return;

		if (typeKind == TypeKind.Array)
		{
			var elementType = CodeEmitterHelpers.GetElementType(unwrapped);
			if (elementType != null)
				CollectTypeRecursive(compilation, elementType, allTypes, reportDiagnostic, discoveredTypeOptions, naming, order);
			return;
		}

		if (typeKind == TypeKind.Object && unwrapped is INamedTypeSymbol namedType)
		{
			RegisterTypeOptions(discoveredTypeOptions, namedType, naming, order);

			if (allTypes.Add(namedType))
			{
				// Analyze the type to collect its properties' types
				var tempTypeInfo = TypeAnalyzer.Analyze(compilation, namedType, null, reportDiagnostic, naming, order);
				if (tempTypeInfo != null)
				{
					foreach (var prop in tempTypeInfo.Properties)
					{
						CollectTypeRecursive(compilation, prop.Type, allTypes, reportDiagnostic, discoveredTypeOptions, naming, order);
					}
				}
			}
		}
	}

	private static void RegisterTypeOptions(Dictionary<ITypeSymbol, (NamingConvention Naming, PropertyOrder Order)> discoveredTypeOptions, ITypeSymbol typeSymbol, NamingConvention naming, PropertyOrder order)
	{
		if (discoveredTypeOptions.ContainsKey(typeSymbol)) return;

		discoveredTypeOptions[typeSymbol] = (naming, order);
	}

	private static ClassDeclarationInfo DetectGeneratedJsonSchemasClass(
		Compilation compilation, 
		string namespaceName,
		Action<Diagnostic> reportDiagnostic)
	{
		// Search for GeneratedJsonSchemas class in this namespace
		var namespaceSymbol = string.IsNullOrEmpty(namespaceName) 
			? compilation.GlobalNamespace 
			: GetNamespaceSymbol(compilation.GlobalNamespace, namespaceName);

		if (namespaceSymbol == null)
			return ClassDeclarationInfo.Default;

		var classSymbol = namespaceSymbol.GetTypeMembers("GeneratedJsonSchemas").FirstOrDefault();
		
		if (classSymbol == null)
			return ClassDeclarationInfo.Default;

		// Check if the class is partial
		var isPartial = classSymbol.DeclaringSyntaxReferences
			.Select(r => r.GetSyntax())
			.OfType<ClassDeclarationSyntax>()
			.Any(c => c.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)));

		if (!isPartial)
		{
			var namespacePart = string.IsNullOrEmpty(namespaceName) 
				? "the global namespace" 
				: $"namespace '{namespaceName}'";
			var diagnostic = Diagnostic.Create(
				Diagnostics.GeneratedJsonSchemasClassMustBePartial,
				classSymbol.Locations.FirstOrDefault(),
				namespacePart);
			reportDiagnostic(diagnostic);
			
			// Still return info but it will cause compilation error
			return ClassDeclarationInfo.Default;
		}

		// Extract modifiers
		var isPublic = classSymbol.DeclaredAccessibility == Accessibility.Public;
		var isInternal = classSymbol.DeclaredAccessibility == Accessibility.Internal;
		var isStatic = classSymbol.IsStatic;

		return new ClassDeclarationInfo
		{
			IsPublic = isPublic,
			IsInternal = isInternal,
			IsStatic = isStatic,
			IsPartial = true
		};
	}

	private static INamespaceSymbol? GetNamespaceSymbol(INamespaceSymbol rootNamespace, string qualifiedName)
	{
		var parts = qualifiedName.Split('.');
		var current = rootNamespace;

		foreach (var part in parts)
		{
			var next = current.GetNamespaceMembers().FirstOrDefault(ns => ns.Name == part);
			if (next == null)
				return null;
			current = next;
		}

		return current;
	}

	private static void ResolvePropertyNameConflicts(List<TypeInfo> types, string rootNamespace)
	{
		var groups = types.GroupBy(t => t.SchemaPropertyName).Where(g => g.Count() > 1);
		foreach (var group in groups)
		{
			foreach (var type in group)
			{
				var relNs = GetRelativeNamespace(type.TypeSymbol, rootNamespace);
				var prefix = string.IsNullOrEmpty(relNs) ? string.Empty : relNs + "_";
				type.ResolvedPropertyName = prefix + type.SchemaPropertyName;
			}
		}

		var usedNames = new HashSet<string>(StringComparer.Ordinal);
		foreach (var type in types.OrderBy(t => t.FullyQualifiedName, StringComparer.Ordinal))
		{
			var baseName = type.ResolvedPropertyName ?? type.SchemaPropertyName;
			var candidate = baseName;
			var index = 2;

			while (!usedNames.Add(candidate))
			{
				candidate = $"{baseName}_{index}";
				index++;
			}

			type.ResolvedPropertyName = candidate == type.SchemaPropertyName ? null : candidate;
		}
	}

	private static string GetRelativeNamespace(ITypeSymbol typeSymbol, string rootNamespace)
	{
		var ns = GetContainingNamespace(typeSymbol);
		if (!string.IsNullOrEmpty(rootNamespace) && ns.StartsWith(rootNamespace))
			ns = ns.Length > rootNamespace.Length ? ns[(rootNamespace.Length + 1)..] : string.Empty;
		return ns.Replace(".", "_");
	}

	private static string GetContainingNamespace(ITypeSymbol typeSymbol)
	{
		var ns = typeSymbol.ContainingNamespace;
		if (ns == null || ns.IsGlobalNamespace) return string.Empty;
		var parts = new List<string>();
		while (ns is { IsGlobalNamespace: false })
		{
			parts.Add(ns.Name);
			ns = ns.ContainingNamespace;
		}
		parts.Reverse();
		return string.Join(".", parts);
	}

	private sealed class TypeToGenerate
	{
		public INamedTypeSymbol TypeSymbol { get; }
		public AttributeData AttributeData { get; }

		public TypeToGenerate(INamedTypeSymbol typeSymbol, AttributeData attributeData)
		{
			TypeSymbol = typeSymbol;
			AttributeData = attributeData;
		}
	}

	private sealed class GenerationOptions
	{
		public required bool IsDisabled { get; init; }
		public required string RootNamespace { get; init; }
		public required NamingConvention DefaultPropertyNaming { get; init; }
		public required PropertyOrder DefaultPropertyOrder { get; init; }
	}
}
