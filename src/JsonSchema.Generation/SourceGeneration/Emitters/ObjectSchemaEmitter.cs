using System.Linq;
using System.Text;

namespace Json.Schema.Generation.SourceGeneration.Emitters;

internal class ObjectSchemaEmitter : ISchemaEmitter
{
	public bool Handles(TypeInfo type) => type.Kind == TypeKind.Object;

	public void EmitSchema(StringBuilder sb, TypeInfo type, string indent, SchemaEmissionContext context)
	{
		sb.AppendLine();
		sb.Append($"{indent}.Type(SchemaValueType.Object)");

		if (type.Properties.Count > 0)
		{
			sb.AppendLine();
			sb.Append($"{indent}.Properties(");
			sb.AppendLine();

			for (int i = 0; i < type.Properties.Count; i++)
			{
				var prop = type.Properties[i];
				sb.Append($"{indent}\t(\"{CodeEmitterHelpers.EscapeString(prop.SchemaName)}\", ");
				
				PropertySchemaEmitter.EmitPropertySchema(sb, prop, indent + "\t", context);
				
				sb.Append(")");
				if (i < type.Properties.Count - 1)
					sb.Append(",");
				sb.AppendLine();
			}

			sb.Append($"{indent})");
		}

		var propertiesWithConditionalRequirements = new System.Collections.Generic.HashSet<string>(
			type.Conditionals
				.SelectMany(c => c.PropertyConsequences)
				.Where(c => c.IsConditionallyRequired)
				.Select(c => c.PropertySchemaName)
				.Distinct());

		var unconditionallyRequiredProps = type.Properties
			.Where(p => p.IsRequired && !propertiesWithConditionalRequirements.Contains(p.SchemaName))
			.ToList();

		if (unconditionallyRequiredProps.Count > 0)
		{
			sb.AppendLine();
			sb.Append($"{indent}.Required(");
			for (int i = 0; i < unconditionallyRequiredProps.Count; i++)
			{
				if (i > 0)
					sb.Append(", ");
				sb.Append($"\"{CodeEmitterHelpers.EscapeString(unconditionallyRequiredProps[i].SchemaName)}\"");
			}
			sb.Append(")");
		}

		ConditionalSchemaEmitter.EmitConditionals(sb, type, indent);

		sb.AppendLine();
		sb.Append($"{indent}.AdditionalProperties(false)");
	}
}
