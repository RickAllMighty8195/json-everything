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

		// Separate attributes by GenericParameter
		var typeAttributes = property.Attributes.Where(a => !a.Parameters.ContainsKey("GenericParameter") || 
		                                                      (a.Parameters.TryGetValue("GenericParameter", out var gp) && ((int)gp!) == -1))
		                             .ToList();
		var itemAttributes = property.Attributes.Where(a => a.Parameters.TryGetValue("GenericParameter", out var gp) && ((int)gp!) == 0)
		                             .ToList();

		// Pass all property attributes to EmitSchemaForType so NumberSchemaEmitter can see JsonNumberHandlingAttribute
		SchemaCodeEmitter.EmitSchemaForType(sb, property.Type, property.IsNullable, indent, context, 
		                                     itemAttributes.Count > 0 ? itemAttributes : null, 
		                                     property.Attributes);
		
		// Emit remaining attributes (excluding JsonNumberHandlingAttribute as it's handled by NumberSchemaEmitter)
		var attributesToEmit = typeAttributes.Where(a => a.AttributeName != "JsonNumberHandlingAttribute").ToList();
		SchemaCodeEmitter.EmitAttributes(sb, attributesToEmit, indent);

		if (!string.IsNullOrWhiteSpace(property.XmlDocSummary) &&
		    property.Attributes.All(a => a.AttributeName != "DescriptionAttribute"))
		{
			sb.AppendLine();
			sb.Append($"{indent}.Description(\"{CodeEmitterHelpers.EscapeString(property.XmlDocSummary!)}\")");
		}

		// Only emit ReadOnly/WriteOnly from property accessors if not already handled by attributes
		if (property.IsReadOnly && !property.Attributes.Any(a => a.AttributeName == "ReadOnlyAttribute"))
		{
			sb.AppendLine();
			sb.Append($"{indent}.ReadOnly(true)");
		}

		if (property.IsWriteOnly && !property.Attributes.Any(a => a.AttributeName == "WriteOnlyAttribute"))
		{
			sb.AppendLine();
			sb.Append($"{indent}.WriteOnly(true)");
		}
	}
}
