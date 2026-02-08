using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Json.Schema.Generation.SourceGeneration.Emitters;

internal class EnumSchemaEmitter : ISchemaEmitter
{
	public bool Handles(TypeInfo type) => type.Kind == TypeKind.Enum;

	public void EmitSchema(StringBuilder sb, TypeInfo type, string indent, SchemaEmissionContext context)
	{
		var enumValues = new List<string>();
		foreach (var member in type.TypeSymbol.GetMembers())
		{
			if (member is IFieldSymbol { IsConst: true, HasConstantValue: true } field) 
				enumValues.Add(field.Name);
		}

		if (enumValues.Count <= 0) return;

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
