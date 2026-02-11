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
}
