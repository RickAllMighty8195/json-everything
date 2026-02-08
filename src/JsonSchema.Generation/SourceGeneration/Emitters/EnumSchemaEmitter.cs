using System.Text;

namespace Json.Schema.Generation.SourceGeneration.Emitters;

internal class EnumSchemaEmitter : ISchemaEmitter
{
	public bool Handles(TypeInfo type) => type.Kind == TypeKind.Enum;

	public void EmitSchema(StringBuilder sb, TypeInfo type, string indent)
	{
		sb.AppendLine();
		sb.Append($"{indent}.Type(SchemaValueType.String)");
		
		if (type.EnumValues.Count > 0)
		{
			sb.AppendLine();
			sb.Append($"{indent}.Enum(");
			for (int i = 0; i < type.EnumValues.Count; i++)
			{
				if (i > 0)
					sb.Append(", ");
				sb.Append($"\"{CodeEmitterHelpers.EscapeString(type.EnumValues[i])}\"");
			}
			sb.Append(')');
		}
	}
}
