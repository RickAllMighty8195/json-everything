using System.Linq;
using System.Text;

namespace Json.Schema.Generation.SourceGeneration.Emitters;

internal class NumberSchemaEmitter : ISchemaEmitter
{
	public bool Handles(TypeInfo type) => type.Kind == TypeKind.Number;

	public void EmitSchema(StringBuilder sb, TypeInfo type, string indent, SchemaEmissionContext context)
	{
		var jsonNumberHandling = type.PropertyAttributes?
			.FirstOrDefault(a => a.AttributeName == "JsonNumberHandlingAttribute");
		
		var handling = 0;
		if (jsonNumberHandling != null && jsonNumberHandling.Parameters.TryGetValue("arg0", out var handlingValue)) 
			handling = (int)handlingValue!;
		
		var allowReadingFromString = (handling & 1) != 0;
		var allowNamedFloatingPoint = (handling & 4) != 0;
		
		if (allowNamedFloatingPoint)
		{
			sb.AppendLine();
			sb.Append($"{indent}.AnyOf(");
			sb.AppendLine();
			
			sb.Append($"{indent}\tnew JsonSchemaBuilder()");
			sb.AppendLine();
			if (type.IsNullable)
			{
				if (allowReadingFromString)
					sb.Append($"{indent}\t\t.Type(SchemaValueType.Number, SchemaValueType.String, SchemaValueType.Null)");
				else
					sb.Append($"{indent}\t\t.Type(SchemaValueType.Number, SchemaValueType.Null)");
			}
			else
			{
				if (allowReadingFromString)
					sb.Append($"{indent}\t\t.Type(SchemaValueType.Number, SchemaValueType.String)");
				else
					sb.Append($"{indent}\t\t.Type(SchemaValueType.Number)");
			}
			sb.Append(",");
			sb.AppendLine();
			
			sb.Append($"{indent}\tnew JsonSchemaBuilder()");
			sb.AppendLine();
			sb.Append($"{indent}\t\t.Enum(\"NaN\", \"Infinity\", \"-Infinity\")");
			sb.AppendLine();
			sb.Append($"{indent})");
		}
		else if (allowReadingFromString)
		{
			sb.AppendLine();
			if (type.IsNullable)
				sb.Append($"{indent}.Type(SchemaValueType.Number, SchemaValueType.String, SchemaValueType.Null)");
			else
				sb.Append($"{indent}.Type(SchemaValueType.Number, SchemaValueType.String)");
		}
		else
		{
			sb.AppendLine();
			if (type.IsNullable)
				sb.Append($"{indent}.Type(SchemaValueType.Number, SchemaValueType.Null)");
			else
				sb.Append($"{indent}.Type(SchemaValueType.Number)");
		}
	}
}
