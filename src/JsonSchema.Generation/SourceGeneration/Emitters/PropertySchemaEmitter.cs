using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Json.Schema.Generation.SourceGeneration.Emitters;

/// <summary>
/// Emits schema code for properties.
/// </summary>
internal static class PropertySchemaEmitter
{
	public static void EmitPropertySchema(StringBuilder sb, PropertyInfo property, string indent)
	{
		sb.Append("new JsonSchemaBuilder()");

		var typeKind = GetPropertyTypeKind(property.Type);

		// Handle nullability
		bool isNullable = property.IsNullable;

		if (isNullable)
		{
			sb.AppendLine();
			sb.Append($"{indent}.Type(");
			EmitJsonSchemaType(sb, typeKind);
			sb.Append(", SchemaValueType.Null)");
		}
		else
		{
			sb.AppendLine();
			sb.Append($"{indent}.Type(");
			EmitJsonSchemaType(sb, typeKind);
			sb.Append(")");
		}

		// Add format for special types
		switch (typeKind)
		{
			case TypeKind.DateTime:
				sb.AppendLine();
				sb.Append($"{indent}.Format(Formats.DateTime)");
				break;
			case TypeKind.Guid:
				sb.AppendLine();
				sb.Append($"{indent}.Format(Formats.Uuid)");
				break;
			case TypeKind.Uri:
				sb.AppendLine();
				sb.Append($"{indent}.Format(Formats.Uri)");
				break;
		}

		// Emit property-level attributes
		EmitAttributes(sb, property.Attributes, indent);

		// Emit description from XML doc
		if (!string.IsNullOrWhiteSpace(property.XmlDocSummary) && 
		    !property.Attributes.Any(a => a.AttributeName == "DescriptionAttribute"))
		{
			sb.AppendLine();
			sb.Append($"{indent}.Description(\"{CodeEmitterHelpers.EscapeString(property.XmlDocSummary)}\")");
		}

		// ReadOnly/WriteOnly
		if (property.IsReadOnly)
		{
			sb.AppendLine();
			sb.Append($"{indent}.ReadOnly(true)");
		}

		if (property.IsWriteOnly)
		{
			sb.AppendLine();
			sb.Append($"{indent}.WriteOnly(true)");
		}

		// Don't call .Build() - Properties() expects JsonSchemaBuilder not JsonSchema
	}

	private static void EmitJsonSchemaType(StringBuilder sb, TypeKind kind)
	{
		var typeValue = kind switch
		{
			TypeKind.Boolean => "SchemaValueType.Boolean",
			TypeKind.Integer => "SchemaValueType.Integer",
			TypeKind.Number => "SchemaValueType.Number",
			TypeKind.String or TypeKind.DateTime or TypeKind.Guid or TypeKind.Uri or TypeKind.Enum => "SchemaValueType.String",
			TypeKind.Array => "SchemaValueType.Array",
			TypeKind.Object => "SchemaValueType.Object",
			_ => "SchemaValueType.Object"
		};

		sb.Append(typeValue);
	}

	private static TypeKind GetPropertyTypeKind(ITypeSymbol typeSymbol)
	{
		var unwrappedType = CodeEmitterHelpers.UnwrapNullable(typeSymbol);
		var typeString = unwrappedType.ToDisplayString();

		if (typeString == "bool" || typeString == "System.Boolean")
			return TypeKind.Boolean;

		if (typeString is "byte" or "sbyte" or "short" or "ushort" or "int" or "uint" or "long" or "ulong" or
		    "System.Byte" or "System.SByte" or "System.Int16" or "System.UInt16" or
		    "System.Int32" or "System.UInt32" or "System.Int64" or "System.UInt64")
			return TypeKind.Integer;

		if (typeString is "float" or "double" or "decimal" or "System.Single" or "System.Double" or "System.Decimal")
			return TypeKind.Number;

		if (typeString is "string" or "System.String")
			return TypeKind.String;

		if (typeString is "System.DateTime" or "System.DateTimeOffset")
			return TypeKind.DateTime;

		if (typeString == "System.Guid")
			return TypeKind.Guid;

		if (typeString == "System.Uri")
			return TypeKind.Uri;

		if (unwrappedType.TypeKind == Microsoft.CodeAnalysis.TypeKind.Enum)
			return TypeKind.Enum;

		if (unwrappedType is IArrayTypeSymbol || CodeEmitterHelpers.IsCollectionType(unwrappedType))
			return TypeKind.Array;

		return TypeKind.Object;
	}

	private static void EmitAttributes(StringBuilder sb, List<AttributeInfo> attributes, string indent)
	{
		foreach (var attr in attributes)
		{
			// Handle custom emitter attributes
			if (attr.IsCustomEmitter && attr.AttributeFullName != null)
			{
				sb.AppendLine();
				sb.Append($"{indent}");
				EmitCustomAttributeCall(sb, attr);
				continue;
			}
			
			// Handle built-in validation attributes
			switch (attr.AttributeName)
			{
				case "MinimumAttribute" when attr.Parameters.TryGetValue("arg0", out var minValue):
					sb.AppendLine();
					sb.Append($"{indent}.Minimum({CodeEmitterHelpers.FormatValue(minValue)})");
					break;
				case "MaximumAttribute" when attr.Parameters.TryGetValue("arg0", out var maxValue):
					sb.AppendLine();
					sb.Append($"{indent}.Maximum({CodeEmitterHelpers.FormatValue(maxValue)})");
					break;
				case "ExclusiveMinimumAttribute" when attr.Parameters.TryGetValue("arg0", out var exMinValue):
					sb.AppendLine();
					sb.Append($"{indent}.ExclusiveMinimum({CodeEmitterHelpers.FormatValue(exMinValue)})");
					break;
				case "ExclusiveMaximumAttribute" when attr.Parameters.TryGetValue("arg0", out var exMaxValue):
					sb.AppendLine();
					sb.Append($"{indent}.ExclusiveMaximum({CodeEmitterHelpers.FormatValue(exMaxValue)})");
					break;
				case "MinLengthAttribute" when attr.Parameters.TryGetValue("arg0", out var minLen):
					sb.AppendLine();
					sb.Append($"{indent}.MinLength({CodeEmitterHelpers.FormatValue(minLen)})");
					break;
				case "MaxLengthAttribute" when attr.Parameters.TryGetValue("arg0", out var maxLen):
					sb.AppendLine();
					sb.Append($"{indent}.MaxLength({CodeEmitterHelpers.FormatValue(maxLen)})");
					break;
				case "PatternAttribute" when attr.Parameters.TryGetValue("arg0", out var pattern):
					sb.AppendLine();
					sb.Append($"{indent}.Pattern(\"{CodeEmitterHelpers.EscapeString(pattern?.ToString() ?? "")}\")");
					break;
				case "MinItemsAttribute" when attr.Parameters.TryGetValue("arg0", out var minItems):
					sb.AppendLine();
					sb.Append($"{indent}.MinItems({CodeEmitterHelpers.FormatValue(minItems)})");
					break;
				case "MaxItemsAttribute" when attr.Parameters.TryGetValue("arg0", out var maxItems):
					sb.AppendLine();
					sb.Append($"{indent}.MaxItems({CodeEmitterHelpers.FormatValue(maxItems)})");
					break;
				case "UniqueItemsAttribute":
					sb.AppendLine();
					sb.Append($"{indent}.UniqueItems(true)");
					break;
				case "MultipleOfAttribute" when attr.Parameters.TryGetValue("arg0", out var multipleOf):
					sb.AppendLine();
					sb.Append($"{indent}.MultipleOf({CodeEmitterHelpers.FormatValue(multipleOf)})");
					break;
				case "TitleAttribute" when attr.Parameters.TryGetValue("arg0", out var title):
					sb.AppendLine();
					sb.Append($"{indent}.Title(\"{CodeEmitterHelpers.EscapeString(title?.ToString() ?? "")}\")");
					break;
				case "DescriptionAttribute" when attr.Parameters.TryGetValue("arg0", out var description):
					sb.AppendLine();
					sb.Append($"{indent}.Description(\"{CodeEmitterHelpers.EscapeString(description?.ToString() ?? "")}\")");
					break;
				case "ObsoleteAttribute":
					sb.AppendLine();
					sb.Append($"{indent}.Deprecated(true)");
					break;
			}
		}
	}
	
	private static void EmitCustomAttributeCall(StringBuilder sb, AttributeInfo attr)
	{
		// Call the extension method with parameters
		var lastDot = attr.AttributeFullName!.LastIndexOf('.');
		var attrName = lastDot >= 0 ? attr.AttributeFullName.Substring(lastDot + 1) : attr.AttributeFullName;
		if (attrName.EndsWith("Attribute"))
			attrName = attrName.Substring(0, attrName.Length - 9);

		sb.Append($".{attrName}(");
		
		// Pass constructor arguments
		bool first = true;
		for (int i = 0; i < attr.Parameters.Count; i++)
		{
			if (attr.Parameters.TryGetValue($"arg{i}", out var value))
			{
				if (!first) sb.Append(", ");
				sb.Append(CodeEmitterHelpers.FormatValue(value));
				first = false;
			}
		}
		
		sb.Append(")");
	}
}
