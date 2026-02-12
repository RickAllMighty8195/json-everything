using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Json.Schema.Generation.SourceGeneration;

/// <summary>
/// Source generator that creates static JSON schemas for types decorated with [GenerateJsonSchema].
/// </summary>
[Generator]
public class JsonSchemaSourceGenerator : IIncrementalGenerator
{
	private const string _generateJsonSchemaAttributeName = "Json.Schema.Generation.Serialization.GenerateJsonSchemaAttribute";

	/// <summary>
	/// Initializes the incremental generator.
	/// </summary>
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var typesWithAttribute = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				fullyQualifiedMetadataName: _generateJsonSchemaAttributeName,
				predicate: static (node, _) => node is TypeDeclarationSyntax,
				transform: static (ctx, _) => GetTypeToGenerate(ctx))
			.Where(static type => type is not null);

		var compilationAndTypes = context.CompilationProvider.Combine(typesWithAttribute.Collect());

		context.RegisterSourceOutput(compilationAndTypes, static (spc, source) => Execute(source.Left, source.Right!, spc));
	}

	private static TypeToGenerate? GetTypeToGenerate(GeneratorAttributeSyntaxContext context)
	{
		if (context.TargetSymbol is not INamedTypeSymbol typeSymbol) return null;

		var attributeData = context.Attributes.FirstOrDefault();
		if (attributeData == null) return null;

		return new TypeToGenerate(typeSymbol, attributeData);
	}

	private static void Execute(Compilation compilation, ImmutableArray<TypeToGenerate?> types, SourceProductionContext context)
	{
		if (types.IsDefaultOrEmpty) return;

		var validTypes = types.Where(t => t != null).Select(t => t!).ToList();
		if (validTypes.Count == 0) return;

		var analyzedTypes = new List<TypeInfo>();
		foreach (var type in validTypes)
		{
			var typeInfo = TypeAnalyzer.Analyze(compilation, type.TypeSymbol, type.AttributeData, context.ReportDiagnostic);
			if (typeInfo != null) 
				analyzedTypes.Add(typeInfo);
		}

		if (analyzedTypes.Count == 0) return;

		var typesByNamespace = analyzedTypes.GroupBy(t => GetNamespace(t.TypeSymbol));

		foreach (var group in typesByNamespace)
		{
			var namespaceName = group.Key;
			var typesInNamespace = group.ToList();

			// Detect user-defined GeneratedJsonSchemas class
			var classDeclaration = DetectGeneratedJsonSchemasClass(compilation, namespaceName, context.ReportDiagnostic);

			var generatedCode = SchemaCodeEmitter.EmitGeneratedClass(typesInNamespace, namespaceName, classDeclaration);

			var safeName = string.IsNullOrEmpty(namespaceName) 
				? "GeneratedJsonSchemas" 
				: namespaceName.Replace(".", "_");
			var fileName = $"{safeName}.g.cs";

			context.AddSource(fileName, SourceText.From(generatedCode, Encoding.UTF8));
		}
	}

	private static string GetNamespace(ISymbol symbol)
	{
		if (symbol.ContainingNamespace == null || symbol.ContainingNamespace.IsGlobalNamespace)
			return string.Empty;

		var namespaces = new List<string>();
		var currentNamespace = symbol.ContainingNamespace;

		while (currentNamespace is { IsGlobalNamespace: false })
		{
			namespaces.Add(currentNamespace.Name);
			currentNamespace = currentNamespace.ContainingNamespace;
		}

		namespaces.Reverse();
		return string.Join(".", namespaces);
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
}
