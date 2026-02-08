using System.Collections.Generic;
using System.Linq;
using System.Text;
using Json.Schema.Generation.Serialization;
using Microsoft.CodeAnalysis;

namespace Json.Schema.Generation.SourceGeneration.Emitters;

/// <summary>
/// Emits schema code for properties.
/// </summary>
internal static class PropertySchemaEmitter
{
	public static void EmitPropertySchema(StringBuilder sb, PropertyInfo property, string indent, SchemaEmissionContext context)
	{
		sb.Append("new JsonSchemaBuilder()");

		// Emit the type schema (may use $ref if in defs)
		SchemaCodeEmitter.EmitSchemaForType(sb, property.Type, property.IsNullable, indent, context);

		// Emit property-level attributes
		SchemaCodeEmitter.EmitAttributes(sb, property.Attributes, indent);

		// Emit description from XML doc
		if (!string.IsNullOrWhiteSpace(property.XmlDocSummary) && 
		    !property.Attributes.Any(a => a.AttributeName == "DescriptionAttribute"))
		{
			sb.AppendLine();
			sb.Append($"{indent}.Description(\"{CodeEmitterHelpers.EscapeString(property.XmlDocSummary)}\")");
		}

		// ReadOnly/WriteOnly
		if (property.IsReadOnly)
		{
			sb.AppendLine();
			sb.Append($"{indent}.ReadOnly(true)");
		}

		if (property.IsWriteOnly)
		{
			sb.AppendLine();
			sb.Append($"{indent}.WriteOnly(true)");
		}

		// Don't call .Build() - Properties() expects JsonSchemaBuilder not JsonSchema
	}
}
