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

		var requiredProps = type.Properties.Where(p => p.IsRequired).ToList();
		if (requiredProps.Count > 0)
		{
			sb.AppendLine();
			sb.Append($"{indent}.Required(");
			for (int i = 0; i < requiredProps.Count; i++)
			{
				if (i > 0)
					sb.Append(", ");
				sb.Append($"\"{CodeEmitterHelpers.EscapeString(requiredProps[i].SchemaName)}\"");
			}
			sb.Append(")");
		}

		sb.AppendLine();
		sb.Append($"{indent}.AdditionalProperties(false)");
	}
}
