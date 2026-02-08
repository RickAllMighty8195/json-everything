using System;
using System.Collections.Generic;
using System.Linq;
using Json.Schema.Generation.Serialization;
using Microsoft.CodeAnalysis;

namespace Json.Schema.Generation.SourceGeneration;

internal static class TypeAnalyzer
{
	private static readonly HashSet<string> _supportedValidationAttributes =
	[
		"MinimumAttribute",
		"MaximumAttribute",
		"ExclusiveMinimumAttribute",
		"ExclusiveMaximumAttribute",
		"MinLengthAttribute",
		"MaxLengthAttribute",
		"PatternAttribute",
		"MinItemsAttribute",
		"MaxItemsAttribute",
		"UniqueItemsAttribute",
		"MultipleOfAttribute",
		"TitleAttribute",
		"DescriptionAttribute",
		"RequiredAttribute",
		"ObsoleteAttribute"
	];

	public static TypeInfo? Analyze(INamedTypeSymbol typeSymbol, AttributeData attributeData, Action<Diagnostic> reportDiagnostic)
	{
		if (typeSymbol is { IsGenericType: true, IsUnboundGenericType: false })
		{
			foreach (var typeArg in typeSymbol.TypeArguments)
			{
				if (typeArg.TypeKind == Microsoft.CodeAnalysis.TypeKind.TypeParameter)
				{
					reportDiagnostic(Diagnostic.Create(
						Diagnostics.OpenGenericTypeNotSupported,
						typeSymbol.Locations.FirstOrDefault(),
						typeSymbol.ToDisplayString()));

					return null;
				}
			}
		}

		var propertyNaming = NamingConvention.AsDeclared;
		var propertyOrder = PropertyOrder.AsDeclared;

		foreach (var namedArg in attributeData.NamedArguments)
		{
			switch (namedArg.Key)
			{
				case "PropertyNaming" when namedArg.Value.Value is int namingValue:
					propertyNaming = (NamingConvention)namingValue;
					break;
				case "PropertyOrder" when namedArg.Value.Value is int orderValue:
					propertyOrder = (PropertyOrder)orderValue;
					break;
			}
		}

		var typeKind = DetermineTypeKind(typeSymbol);
		var isNullable = IsNullableType(typeSymbol);

		var typeInfo = new TypeInfo
		{
			TypeSymbol = typeSymbol,
			FullyQualifiedName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			SchemaPropertyName = GetSchemaPropertyName(typeSymbol),
			PropertyNaming = propertyNaming,
			PropertyOrder = propertyOrder,
			Kind = typeKind,
			IsNullable = isNullable,
			XmlDocSummary = GetXmlDocSummary(typeSymbol)
		};

		switch (typeKind)
		{
			case TypeKind.Object:
				AnalyzeObjectType(typeInfo, reportDiagnostic);
				break;
			case TypeKind.Enum:
				AnalyzeEnumType(typeInfo);
				break;
			case TypeKind.Array:
				break;
		}

		ExtractAttributes(typeSymbol.GetAttributes(), typeInfo.TypeAttributes, reportDiagnostic);

		return typeInfo;
	}

	private static TypeKind DetermineTypeKind(ITypeSymbol typeSymbol)
	{
		var unwrappedType = UnwrapNullable(typeSymbol);
		var typeString = unwrappedType.ToDisplayString();

		switch (typeString)
		{
			case "bool" or "System.Boolean":
				return TypeKind.Boolean;
			case "byte" or "sbyte" or "short" or "ushort" or "int" or "uint" or "long" or "ulong" or
				"System.Byte" or "System.SByte" or "System.Int16" or "System.UInt16" or
				"System.Int32" or "System.UInt32" or "System.Int64" or "System.UInt64":
				return TypeKind.Integer;
			case "float" or "double" or "decimal" or "System.Single" or "System.Double" or "System.Decimal":
				return TypeKind.Number;
			case "string" or "System.String":
				return TypeKind.String;
			case "System.DateTime" or "System.DateTimeOffset":
				return TypeKind.DateTime;
			case "System.Guid":
				return TypeKind.Guid;
			case "System.Uri":
				return TypeKind.Uri;
		}

		if (unwrappedType.TypeKind == Microsoft.CodeAnalysis.TypeKind.Enum) return TypeKind.Enum;
		if (unwrappedType is IArrayTypeSymbol) return TypeKind.Array;
		if (unwrappedType is INamedTypeSymbol namedType && IsCollectionType(namedType)) return TypeKind.Array;

		return TypeKind.Object;
	}

	private static bool IsCollectionType(INamedTypeSymbol typeSymbol)
	{
		if (!typeSymbol.IsGenericType)
			return false;

		var typeString = typeSymbol.ConstructedFrom.ToDisplayString();
		return typeString is
			"System.Collections.Generic.List<T>" or
			"System.Collections.Generic.IList<T>" or
			"System.Collections.Generic.ICollection<T>" or
			"System.Collections.Generic.IEnumerable<T>" or
			"System.Collections.Generic.IReadOnlyList<T>" or
			"System.Collections.Generic.IReadOnlyCollection<T>";
	}

	private static void AnalyzeObjectType(TypeInfo typeInfo, Action<Diagnostic> reportDiagnostic)
	{
		var typeSymbol = typeInfo.TypeSymbol;

		var members = typeSymbol.GetMembers()
			.Where(m => !m.IsStatic && (m.Kind == SymbolKind.Property || m.Kind == SymbolKind.Field))
			.ToList();

		foreach (var member in members)
		{
			if (HasAttribute(member, "JsonIgnoreAttribute")) continue;

			ITypeSymbol memberType;
			var isReadOnly = false;
			var isWriteOnly = false;

			if (member is IPropertySymbol property)
			{
				if (property.DeclaredAccessibility != Accessibility.Public && !HasAttribute(member, "JsonIncludeAttribute")) continue;

				if (property.IsIndexer) continue;

				memberType = property.Type;
				isReadOnly = property.SetMethod is not { DeclaredAccessibility: Accessibility.Public };
				isWriteOnly = property.GetMethod is not { DeclaredAccessibility: Accessibility.Public };

				if (isWriteOnly) continue;
			}
			else if (member is IFieldSymbol field)
			{
				if (field.DeclaredAccessibility != Accessibility.Public && !HasAttribute(member, "JsonIncludeAttribute")) continue;

				memberType = field.Type;
				isReadOnly = field.IsReadOnly;
			}
			else continue;

			var schemaName = GetPropertySchemaName(member, typeInfo.PropertyNaming);

			var isRequired = HasAttribute(member, "RequiredAttribute") || 
			                 HasAttribute(member, "System.ComponentModel.DataAnnotations.RequiredAttribute");
			if (!isRequired && member is IPropertySymbol propertySymbol) 
				isRequired = propertySymbol.IsRequired;

			var isNullable = IsNullableType(memberType);

			var propertyInfo = new PropertyInfo
			{
				Name = member.Name,
				SchemaName = schemaName,
				Type = memberType,
				IsRequired = isRequired,
				IsNullable = isNullable,
				IsReadOnly = isReadOnly,
				IsWriteOnly = isWriteOnly,
				XmlDocSummary = GetXmlDocSummary(member)
			};

			ExtractAttributes(member.GetAttributes(), propertyInfo.Attributes, reportDiagnostic);

			typeInfo.Properties.Add(propertyInfo);
		}

		if (typeInfo.PropertyOrder == PropertyOrder.ByName) 
			typeInfo.Properties.Sort((a, b) => string.Compare(a.SchemaName, b.SchemaName, StringComparison.OrdinalIgnoreCase));
	}

	private static void AnalyzeEnumType(TypeInfo typeInfo)
	{
		var typeSymbol = typeInfo.TypeSymbol;

		foreach (var member in typeSymbol.GetMembers())
		{
			if (member is IFieldSymbol { IsConst: true, HasConstantValue: true } field)
			{
				if (HasAttribute(field, "JsonIgnoreAttribute")) continue;

				var enumName = field.Name;
				typeInfo.EnumValues.Add(enumName);
			}
		}
	}

	private static void ExtractAttributes(IEnumerable<AttributeData> attributes, List<AttributeInfo> targetList, Action<Diagnostic> reportDiagnostic)
	{
		foreach (var attr in attributes)
		{
			var attrClass = attr.AttributeClass;
			if (attrClass == null) continue;
			
			var attrName = attrClass.Name;
			var isCustomEmitter = ImplementsInterface(attrClass, "IAttributeHandler") && HasStaticApplyMethod(attrClass);
			if (!isCustomEmitter && !_supportedValidationAttributes.Contains(attrName)) continue;

			List<ApplyParameterInfo>? applyParams = null;
			if (isCustomEmitter) 
				applyParams = ExtractApplyMethodParameters(attrClass);

			var attrInfo = new AttributeInfo
			{
				AttributeName = attrName,
				IsCustomEmitter = isCustomEmitter,
				AttributeFullName = isCustomEmitter ? attrClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) : null,
				ApplyMethodParameters = applyParams
			};

			for (int i = 0; i < attr.ConstructorArguments.Length; i++)
			{
				var arg = attr.ConstructorArguments[i];
				attrInfo.Parameters[$"arg{i}"] = arg.Value;
			}

			foreach (var namedArg in attr.NamedArguments)
			{
				attrInfo.Parameters[namedArg.Key] = namedArg.Value.Value;
			}

			targetList.Add(attrInfo);
		}
	}
	
	private static List<ApplyParameterInfo>? ExtractApplyMethodParameters(INamedTypeSymbol attrClass)
	{
		var applyMethod = attrClass.GetMembers("Apply")
			.OfType<IMethodSymbol>()
			.FirstOrDefault(m => m is { IsStatic: true, Name: "Apply" });
		
		if (applyMethod == null) return null;
		
		var parameters = new List<ApplyParameterInfo>();
		foreach (var param in applyMethod.Parameters.Skip(1))
		{
			parameters.Add(new ApplyParameterInfo
			{
				Name = param.Name,
				TypeName = param.Type.ToDisplayString()
			});
		}
		
		return parameters;
	}
	
	private static bool ImplementsInterface(INamedTypeSymbol typeSymbol, string interfaceName) => 
		typeSymbol.AllInterfaces.Any(i => i.Name == interfaceName);

	private static bool HasStaticApplyMethod(INamedTypeSymbol typeSymbol) =>
		typeSymbol.GetMembers("Apply")
			.OfType<IMethodSymbol>()
			.Any(m => m.IsStatic && m.Name == "Apply" && 
			          m.Parameters.Length > 0 && 
			          m.Parameters[0].Type.Name == "JsonSchemaBuilder");

	private static string GetPropertySchemaName(ISymbol member, NamingConvention naming)
	{
		var jsonPropertyNameAttr = member.GetAttributes()
			.FirstOrDefault(a => a.AttributeClass?.Name == "JsonPropertyNameAttribute");

		return jsonPropertyNameAttr is { ConstructorArguments.Length: > 0 }
			? jsonPropertyNameAttr.ConstructorArguments[0].Value?.ToString() ?? member.Name
			: ApplyNamingConvention(member.Name, naming);
	}

	private static string ApplyNamingConvention(string name, NamingConvention naming) =>
		naming switch
		{
			NamingConvention.CamelCase => ToCamelCase(name),
			NamingConvention.PascalCase => ToPascalCase(name),
			NamingConvention.SnakeCase => ToSnakeCase(name),
			NamingConvention.LowerSnakeCase => ToSnakeCase(name).ToLowerInvariant(),
			NamingConvention.UpperSnakeCase => ToSnakeCase(name).ToUpperInvariant(),
			NamingConvention.KebabCase => ToKebabCase(name),
			NamingConvention.UpperKebabCase => ToKebabCase(name).ToUpperInvariant(),
			_ => name
		};

	private static string ToCamelCase(string name) =>
		string.IsNullOrEmpty(name) || char.IsLower(name[0])
			? name
			: char.ToLowerInvariant(name[0]) + name[1..];

	private static string ToPascalCase(string name) =>
		string.IsNullOrEmpty(name) || char.IsUpper(name[0])
			? name
			: char.ToUpperInvariant(name[0]) + name[1..];

	private static string ToSnakeCase(string name)
	{
		if (string.IsNullOrEmpty(name)) return name;

		var result = new System.Text.StringBuilder();
		result.Append(char.ToLowerInvariant(name[0]));

		for (int i = 1; i < name.Length; i++)
		{
			if (char.IsUpper(name[i]))
			{
				result.Append('_');
				result.Append(char.ToLowerInvariant(name[i]));
			}
			else
				result.Append(name[i]);
		}

		return result.ToString();
	}

	private static string ToKebabCase(string name) => ToSnakeCase(name).Replace('_', '-');

	private static string GetSchemaPropertyName(INamedTypeSymbol typeSymbol) =>
		typeSymbol.ContainingType != null
			? $"{GetSchemaPropertyName(typeSymbol.ContainingType)}_{typeSymbol.Name}"
			: typeSymbol.Name;

	private static bool IsNullableType(ITypeSymbol typeSymbol) =>
		typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T ||
		typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;

	private static ITypeSymbol UnwrapNullable(ITypeSymbol typeSymbol) =>
		typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
		typeSymbol is INamedTypeSymbol namedType
			? namedType.TypeArguments[0]
			: typeSymbol;

	private static bool HasAttribute(ISymbol symbol, string attributeName) => 
		symbol.GetAttributes().Any(a => a.AttributeClass?.Name == attributeName);

	private static string? GetXmlDocSummary(ISymbol symbol)
	{
		var xml = symbol.GetDocumentationCommentXml();
		if (string.IsNullOrWhiteSpace(xml)) return null;

		var summaryStart = xml!.IndexOf("<summary>", StringComparison.Ordinal);
		var summaryEnd = xml.IndexOf("</summary>", StringComparison.Ordinal);

		if (summaryStart >= 0 && summaryEnd > summaryStart)
		{
			var summaryText = xml.Substring(summaryStart + 9, summaryEnd - summaryStart - 9);
			return summaryText.Trim();
		}

		return null;
	}
}
