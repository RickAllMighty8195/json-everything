using System.Linq;
using System.Text;

namespace Json.Schema.Generation.SourceGeneration.Emitters;

/// <summary>
/// Emits schema code for properties.
/// </summary>
internal static class PropertySchemaEmitter
{
	public static void EmitPropertySchema(StringBuilder sb, PropertyInfo property, string indent, SchemaEmissionContext context)
	{
		sb.Append("new JsonSchemaBuilder()");

		SchemaCodeEmitter.EmitSchemaForType(sb, property.Type, property.IsNullable, indent, context);
		SchemaCodeEmitter.EmitAttributes(sb, property.Attributes, indent);

		if (!string.IsNullOrWhiteSpace(property.XmlDocSummary) &&
		    property.Attributes.All(a => a.AttributeName != "DescriptionAttribute"))
		{
			sb.AppendLine();
			sb.Append($"{indent}.Description(\"{CodeEmitterHelpers.EscapeString(property.XmlDocSummary!)}\")");
		}

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
	}
}
