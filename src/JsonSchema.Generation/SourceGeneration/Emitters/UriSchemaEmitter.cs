using System.Text;

namespace Json.Schema.Generation.SourceGeneration.Emitters;

internal class UriSchemaEmitter : ISchemaEmitter
{
	public bool Handles(TypeInfo type) => type.Kind == TypeKind.Uri;

	public void EmitSchema(StringBuilder sb, TypeInfo type, string indent)
	{
		sb.AppendLine();
		sb.Append($"{indent}.Type(SchemaValueType.String)");
		sb.AppendLine();
		sb.Append($"{indent}.Format(Formats.Uri)");
	}
}
