using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Json.Schema.Generation.SourceGeneration.Emitters;

internal class DictionarySchemaEmitter : ISchemaEmitter
{
	public bool Handles(TypeInfo type) => type.Kind == TypeKind.Dictionary;

	public void EmitSchema(StringBuilder sb, TypeInfo type, string indent, SchemaEmissionContext context)
	{
		sb.AppendLine();
		sb.Append($"{indent}.Type(SchemaValueType.Object)");

		var keyType = CodeEmitterHelpers.GetDictionaryKeyType(type.TypeSymbol);
		if (keyType != null)
			EmitPropertyNames(sb, keyType, indent, context);

		var valueType = CodeEmitterHelpers.GetDictionaryValueType(type.TypeSymbol);
		if (valueType == null) return;

		sb.AppendLine();
		sb.Append($"{indent}.AdditionalProperties(new JsonSchemaBuilder()");

		var isNullableValue = valueType.NullableAnnotation == NullableAnnotation.Annotated ||
		                      valueType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

		SchemaCodeEmitter.EmitSchemaForType(sb, valueType, isNullableValue, indent + "\t", context);
		sb.Append(")");
	}

	private static void EmitPropertyNames(StringBuilder sb, ITypeSymbol keyType, string indent, SchemaEmissionContext context)
	{
		var unwrappedKeyType = CodeEmitterHelpers.UnwrapNullable(keyType);

		if (unwrappedKeyType.TypeKind == Microsoft.CodeAnalysis.TypeKind.Enum && unwrappedKeyType is INamedTypeSymbol enumType)
		{
			var refUri = context.GetRefUri(enumType);
			if (!string.IsNullOrEmpty(refUri))
			{
				sb.AppendLine();
				sb.Append($"{indent}.PropertyNames(new JsonSchemaBuilder().Ref(\"{refUri}\"))");
				return;
			}

			var enumValues = new List<string>();
			foreach (var member in enumType.GetMembers().OfType<IFieldSymbol>())
			{
				if (member is { IsConst: true, HasConstantValue: true } && CodeEmitterHelpers.ShouldIncludeEnumMember(member))
					enumValues.Add(member.Name);
			}

			if (enumValues.Count > 0)
			{
				sb.AppendLine();
				sb.Append($"{indent}.PropertyNames(new JsonSchemaBuilder().Enum(");
				for (int i = 0; i < enumValues.Count; i++)
				{
					if (i > 0) sb.Append(", ");
					sb.Append($"\"{CodeEmitterHelpers.EscapeString(enumValues[i])}\"");
				}
				sb.Append("))");
			}

			return;
		}

		if (unwrappedKeyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Guid")
		{
			sb.AppendLine();
			sb.Append($"{indent}.PropertyNames(new JsonSchemaBuilder().Type(SchemaValueType.String).Format(global::Json.Schema.Formats.Uuid))");
		}
	}
}