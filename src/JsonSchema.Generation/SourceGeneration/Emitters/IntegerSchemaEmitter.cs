using System.Text;

namespace Json.Schema.Generation.SourceGeneration.Emitters;

internal class IntegerSchemaEmitter : ISchemaEmitter
{
	public bool Handles(TypeInfo type) => type.Kind == TypeKind.Integer;

	public void EmitSchema(StringBuilder sb, TypeInfo type, string indent, SchemaEmissionContext context)
	{
		sb.AppendLine();
		if (type.IsNullable)
			sb.Append($"{indent}.Type(SchemaValueType.Integer, SchemaValueType.Null)");
		else
			sb.Append($"{indent}.Type(SchemaValueType.Integer)");
	}
}
