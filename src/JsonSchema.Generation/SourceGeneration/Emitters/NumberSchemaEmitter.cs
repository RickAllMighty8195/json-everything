using System.Text;

namespace Json.Schema.Generation.SourceGeneration.Emitters;

internal class NumberSchemaEmitter : ISchemaEmitter
{
	public bool Handles(TypeInfo type) => type.Kind == TypeKind.Number;

	public void EmitSchema(StringBuilder sb, TypeInfo type, string indent, SchemaEmissionContext context)
	{
		sb.AppendLine();
		if (type.IsNullable)
			sb.Append($"{indent}.Type(SchemaValueType.Number, SchemaValueType.Null)");
		else
			sb.Append($"{indent}.Type(SchemaValueType.Number)");
	}
}
