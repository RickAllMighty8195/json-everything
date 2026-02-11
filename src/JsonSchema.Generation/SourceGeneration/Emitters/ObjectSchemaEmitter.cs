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

		// When StrictConditionals is true, filter out properties that have conditional consequences
		var strictConditionalPropertyNames = new System.Collections.Generic.HashSet<string>();
		if (type.StrictConditionals)
		{
			foreach (var conditional in type.Conditionals)
			{
				foreach (var consequence in conditional.PropertyConsequences)
				{
					if (consequence.ConditionalAttributes.Count > 0 ||
						consequence.IsConditionallyReadOnly ||
						consequence.IsConditionallyWriteOnly)
					{
						strictConditionalPropertyNames.Add(consequence.PropertySchemaName);
					}
				}
			}
		}

		var propertiesToEmit = type.StrictConditionals
			? type.Properties.Where(p => !strictConditionalPropertyNames.Contains(p.SchemaName)).ToList()
			: type.Properties;

		if (propertiesToEmit.Count > 0)
		{
			sb.AppendLine();
			sb.Append($"{indent}.Properties(");
			sb.AppendLine();

			for (int i = 0; i < propertiesToEmit.Count; i++)
			{
				var prop = propertiesToEmit[i];
				sb.Append($"{indent}\t(\"{CodeEmitterHelpers.EscapeString(prop.SchemaName)}\", ");
				
				PropertySchemaEmitter.EmitPropertySchema(sb, prop, indent + "\t", context);
				
				sb.Append(")");
				if (i < propertiesToEmit.Count - 1)
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

		ConditionalSchemaEmitter.EmitConditionals(sb, type, indent, context);

		if (type.StrictConditionals && type.Conditionals.Any(c => c.PropertyConsequences.Count > 0))
		{
			sb.AppendLine();
			sb.Append($"{indent}.UnevaluatedProperties(false)");
		}
	}
}
