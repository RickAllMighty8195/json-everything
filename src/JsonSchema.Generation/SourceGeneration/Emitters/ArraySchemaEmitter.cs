using System.Text;

namespace Json.Schema.Generation.SourceGeneration.Emitters;

internal class ArraySchemaEmitter : ISchemaEmitter
{
	public bool Handles(TypeInfo type) => type.Kind == TypeKind.Array;

	public void EmitSchema(StringBuilder sb, TypeInfo type, string indent)
	{
		sb.AppendLine();
		sb.Append($"{indent}.Type(SchemaValueType.Array)");
		
		// TODO: Emit items schema for element type
		//       This requires recursive type analysis which we'll implement later
		sb.AppendLine();
		sb.Append($"{indent}// TODO: Array items schema");
	}
}
