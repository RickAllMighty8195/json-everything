using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Json.Schema.Generation.SourceGeneration.Emitters;

internal class EnumSchemaEmitter : ISchemaEmitter
{
	public bool Handles(TypeInfo type) => type.Kind == TypeKind.Enum;

	public void EmitSchema(StringBuilder sb, TypeInfo type, string indent, SchemaEmissionContext context)
	{
		sb.AppendLine();
		sb.Append($"{indent}.Type(SchemaValueType.String)");
		
		// Extract enum values from the type symbol
		var enumValues = new List<string>();
		if (type.TypeSymbol is INamedTypeSymbol namedType)
		{
			foreach (var member in namedType.GetMembers())
			{
				if (member is IFieldSymbol { IsConst: true } field && field.HasConstantValue)
				{
					enumValues.Add(field.Name);
				}
			}
		}
		else if (type.EnumValues.Count > 0)
		{
			// Fallback to pre-populated values (for TypeAnalyzer path)
			enumValues.AddRange(type.EnumValues);
		}
		
		if (enumValues.Count > 0)
		{
			sb.AppendLine();
			sb.Append($"{indent}.Enum(");
			for (int i = 0; i < enumValues.Count; i++)
			{
				if (i > 0)
					sb.Append(", ");
				sb.Append($"\"{CodeEmitterHelpers.EscapeString(enumValues[i])}\"");
			}
			sb.Append(')');
		}
	}
}
