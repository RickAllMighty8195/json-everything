using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Json.Schema.Generation.SourceGeneration.Emitters;

internal class EnumSchemaEmitter : ISchemaEmitter
{
	public bool Handles(TypeInfo type) => type.Kind == TypeKind.Enum;

	public void EmitSchema(StringBuilder sb, TypeInfo type, string indent, SchemaEmissionContext context)
	{
		// Use pre-filtered enum values from TypeInfo (already excludes JsonIgnore/JsonExclude members)
		if (type.EnumValues.Count <= 0) return;

		sb.AppendLine();
		sb.Append($"{indent}.Enum(");
		for (int i = 0; i < type.EnumValues.Count; i++)
		{
			if (i > 0)
				sb.Append(", ");
			sb.Append($"\"{CodeEmitterHelpers.EscapeString(type.EnumValues[i])}\"");
		}
		sb.Append(')');
	}
}
