using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
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
			var typeInfo = TypeAnalyzer.Analyze(type.TypeSymbol, type.AttributeData, context.ReportDiagnostic);
			if (typeInfo != null) 
				analyzedTypes.Add(typeInfo);
		}

		if (analyzedTypes.Count == 0) return;

		var typesByNamespace = analyzedTypes.GroupBy(t => GetNamespace(t.TypeSymbol));

		foreach (var group in typesByNamespace)
		{
			var namespaceName = group.Key;
			var typesInNamespace = group.ToList();

			var generatedCode = SchemaCodeEmitter.EmitGeneratedClass(typesInNamespace, namespaceName);

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
