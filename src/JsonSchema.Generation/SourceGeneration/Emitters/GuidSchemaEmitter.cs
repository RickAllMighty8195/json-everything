using System.Text;

namespace Json.Schema.Generation.SourceGeneration.Emitters;

internal class GuidSchemaEmitter : ISchemaEmitter
{
	public bool Handles(TypeInfo type) => type.Kind == TypeKind.Guid;

	public void EmitSchema(StringBuilder sb, TypeInfo type, string indent)
	{
		sb.AppendLine();
		sb.Append($"{indent}.Type(SchemaValueType.String)");
		sb.AppendLine();
		sb.Append($"{indent}.Format(Formats.Uuid)");
	}
}
