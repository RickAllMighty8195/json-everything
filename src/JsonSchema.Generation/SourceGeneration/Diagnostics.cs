using Microsoft.CodeAnalysis;

namespace Json.Schema.Generation.SourceGeneration;

internal static class Diagnostics
{
	private const string _category = "JsonSchemaGeneration";

	public static readonly DiagnosticDescriptor OpenGenericTypeNotSupported = new(
		id: "JSGEN001",
		title: "Open generic types are not supported",
		messageFormat: "The type '{0}' is an open generic type. Source generation only supports concrete types.",
		category: _category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor RecursiveTypeNotSupported = new(
		id: "JSGEN002",
		title: "Recursive types are not fully supported in MVP",
		messageFormat: "The type '{0}' contains a recursive reference to itself. This may not generate correctly.",
		category: _category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor UnsupportedAttribute = new(
		id: "JSGEN003",
		title: "Attribute not supported by source generator",
		messageFormat: "The attribute '{0}' is not supported by source generation and will be ignored",
		category: _category,
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public static readonly DiagnosticDescriptor UnsupportedType = new(
		id: "JSGEN004",
		title: "Type not supported by source generator",
		messageFormat: "The type '{0}' is not supported by source generation",
		category: _category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);
}
