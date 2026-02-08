using System.Text;

namespace Json.Schema.Generation.SourceGeneration.Emitters;

internal class ArraySchemaEmitter : ISchemaEmitter
{
	public bool Handles(TypeInfo type) => type.Kind == TypeKind.Array;

	public void EmitSchema(StringBuilder sb, TypeInfo type, string indent, SchemaEmissionContext context)
	{
		sb.AppendLine();
		sb.Append($"{indent}.Type(SchemaValueType.Array)");
		
		var elementType = CodeEmitterHelpers.GetElementType(type.TypeSymbol);
		if (elementType != null)
		{
			sb.AppendLine();
			sb.Append($"{indent}.Items(");
			sb.Append("new JsonSchemaBuilder()");
			
			SchemaCodeEmitter.EmitSchemaForType(sb, elementType, type.IsNullable, indent + "\t", context);
			
			sb.Append(")");
		}
	}
}
