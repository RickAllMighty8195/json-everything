using System.Text;

namespace Json.Schema.Generation.SourceGeneration.Emitters;

internal class IntegerSchemaEmitter : ISchemaEmitter
{
	public bool Handles(TypeInfo type) => type.Kind == TypeKind.Integer;

	public void EmitSchema(StringBuilder sb, TypeInfo type, string indent)
	{
		sb.AppendLine();
		sb.Append($"{indent}.Type(SchemaValueType.Integer)");
	}
}
