using System.Text;

namespace Json.Schema.Generation.SourceGeneration.Emitters;

internal class BooleanSchemaEmitter : ISchemaEmitter
{
	public bool Handles(TypeInfo type) => type.Kind == TypeKind.Boolean;

	public void EmitSchema(StringBuilder sb, TypeInfo type, string indent)
	{
		sb.AppendLine();
		sb.Append($"{indent}.Type(SchemaValueType.Boolean)");
	}
}
