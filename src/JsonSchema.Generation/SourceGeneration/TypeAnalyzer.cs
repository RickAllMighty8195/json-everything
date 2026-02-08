using System;
using System.Collections.Generic;
using System.Linq;
using Json.Schema.Generation.Serialization;
using Microsoft.CodeAnalysis;

namespace Json.Schema.Generation.SourceGeneration;

/// <summary>
/// Analyzes types marked with [GenerateJsonSchema] to extract schema information.
/// </summary>
internal static class TypeAnalyzer
{
	private static readonly HashSet<string> SupportedValidationAttributes = new()
	{
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
	};

	public static TypeInfo? Analyze(INamedTypeSymbol typeSymbol, AttributeData attributeData, Action<Diagnostic> reportDiagnostic)
	{
		// Check for open generic types
		if (typeSymbol.IsGenericType && !typeSymbol.IsUnboundGenericType)
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

		// Extract attribute parameters
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

		// Analyze based on type kind
		switch (typeKind)
		{
			case SourceGeneration.TypeKind.Object:
				AnalyzeObjectType(typeInfo, reportDiagnostic);
				break;
			case SourceGeneration.TypeKind.Enum:
				AnalyzeEnumType(typeInfo);
				break;
			case SourceGeneration.TypeKind.Array:
				// For arrays/collections, we'll need to analyze element type later
				break;
		}

		// Extract type-level attributes
		ExtractAttributes(typeSymbol.GetAttributes(), typeInfo.TypeAttributes, reportDiagnostic);

		return typeInfo;
	}

	private static TypeKind DetermineTypeKind(ITypeSymbol typeSymbol)
	{
		var unwrappedType = UnwrapNullable(typeSymbol);
		var typeString = unwrappedType.ToDisplayString();

		// Check primitives
		if (typeString is "bool" or "System.Boolean")
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

		// Check for enum
		if (unwrappedType.TypeKind == Microsoft.CodeAnalysis.TypeKind.Enum)
			return TypeKind.Enum;

		// Check for arrays and collections
		if (unwrappedType is IArrayTypeSymbol)
			return TypeKind.Array;

		if (unwrappedType is INamedTypeSymbol namedType)
		{
			// Check for IEnumerable<T>, List<T>, ICollection<T>, etc.
			if (IsCollectionType(namedType))
				return TypeKind.Array;
		}

		// Default to object
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

		// Get all instance members (properties and fields)
		var members = typeSymbol.GetMembers()
			.Where(m => !m.IsStatic && (m.Kind == SymbolKind.Property || m.Kind == SymbolKind.Field))
			.ToList();

		foreach (var member in members)
		{
			// Skip if JsonIgnore is present
			if (HasAttribute(member, "JsonIgnoreAttribute"))
				continue;

			ITypeSymbol memberType;
			bool isReadOnly = false;
			bool isWriteOnly = false;

			if (member is IPropertySymbol property)
			{
				// Skip if not accessible
				if (property.DeclaredAccessibility != Accessibility.Public && !HasAttribute(member, "JsonIncludeAttribute"))
					continue;

				// Skip indexers
				if (property.IsIndexer)
					continue;

				memberType = property.Type;
				isReadOnly = property.SetMethod == null || property.SetMethod.DeclaredAccessibility != Accessibility.Public;
				isWriteOnly = property.GetMethod == null || property.GetMethod.DeclaredAccessibility != Accessibility.Public;

				// Skip write-only properties
				if (isWriteOnly)
					continue;
			}
			else if (member is IFieldSymbol field)
			{
				// Skip if not accessible
				if (field.DeclaredAccessibility != Accessibility.Public && !HasAttribute(member, "JsonIncludeAttribute"))
					continue;

				memberType = field.Type;
				isReadOnly = field.IsReadOnly;
			}
			else
			{
				continue;
			}

			// Determine schema name
			var schemaName = GetPropertySchemaName(member, typeInfo.PropertyNaming);

			// Check if required (C# required keyword or RequiredAttribute)
			bool isRequired = HasAttribute(member, "RequiredAttribute") || 
			                  HasAttribute(member, "System.ComponentModel.DataAnnotations.RequiredAttribute");
			if (!isRequired && member is IPropertySymbol propertySymbol)
			{
				isRequired = propertySymbol.IsRequired;
			}

			// Check if nullable
			bool isNullable = IsNullableType(memberType);

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

			// Extract attributes
			ExtractAttributes(member.GetAttributes(), propertyInfo.Attributes, reportDiagnostic);

			typeInfo.Properties.Add(propertyInfo);
		}

		// Apply property ordering
		if (typeInfo.PropertyOrder == PropertyOrder.ByName)
		{
			typeInfo.Properties.Sort((a, b) => string.Compare(a.SchemaName, b.SchemaName, StringComparison.OrdinalIgnoreCase));
		}
	}

	private static void AnalyzeEnumType(TypeInfo typeInfo)
	{
		var typeSymbol = typeInfo.TypeSymbol;

		foreach (var member in typeSymbol.GetMembers())
		{
			if (member is IFieldSymbol { IsConst: true } field && field.HasConstantValue)
			{
				// Skip if JsonIgnore is present
				if (HasAttribute(field, "JsonIgnoreAttribute"))
					continue;

				// Get the string representation of the enum value
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
			
			// Check if this is a custom emitter attribute (implements IAttributeHandler and has static Apply method)
			bool isCustomEmitter = ImplementsInterface(attrClass, "IAttributeHandler") && HasStaticApplyMethod(attrClass);
			
			// For built-in attributes, check if supported
			if (!isCustomEmitter && !SupportedValidationAttributes.Contains(attrName))
				continue;

			// Extract Apply method parameters for custom emitters
			List<ApplyParameterInfo>? applyParams = null;
			if (isCustomEmitter)
			{
				applyParams = ExtractApplyMethodParameters(attrClass);
			}

			var attrInfo = new AttributeInfo
			{
				AttributeName = attrName,
				IsCustomEmitter = isCustomEmitter,
				AttributeFullName = isCustomEmitter ? attrClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) : null,
				ApplyMethodParameters = applyParams
			};

			// Extract constructor arguments
			for (int i = 0; i < attr.ConstructorArguments.Length; i++)
			{
				var arg = attr.ConstructorArguments[i];
				attrInfo.Parameters[$"arg{i}"] = arg.Value;
			}

			// Extract named arguments
			foreach (var namedArg in attr.NamedArguments)
			{
				attrInfo.Parameters[namedArg.Key] = namedArg.Value.Value;
			}

			targetList.Add(attrInfo);
		}
	}
	
	private static List<ApplyParameterInfo>? ExtractApplyMethodParameters(INamedTypeSymbol attrClass)
	{
		// Find the static Apply method
		var applyMethod = attrClass.GetMembers("Apply")
			.OfType<IMethodSymbol>()
			.FirstOrDefault(m => m.IsStatic && m.Name == "Apply");
		
		if (applyMethod == null) return null;
		
		var parameters = new List<ApplyParameterInfo>();
		// Skip the first parameter (should be JsonSchemaBuilder builder)
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
	
	private static bool ImplementsInterface(INamedTypeSymbol typeSymbol, string interfaceName)
	{
		return typeSymbol.AllInterfaces.Any(i => i.Name == interfaceName);
	}
	
	private static bool HasStaticApplyMethod(INamedTypeSymbol typeSymbol)
	{
		return typeSymbol.GetMembers("Apply")
			.OfType<IMethodSymbol>()
			.Any(m => m.IsStatic && m.Name == "Apply" && 
			         m.Parameters.Length > 0 && 
			         m.Parameters[0].Type.Name == "JsonSchemaBuilder");
	}

	private static string GetPropertySchemaName(ISymbol member, NamingConvention naming)
	{
		// Check for JsonPropertyName attribute
		var jsonPropertyNameAttr = member.GetAttributes()
			.FirstOrDefault(a => a.AttributeClass?.Name == "JsonPropertyNameAttribute");

		if (jsonPropertyNameAttr != null && jsonPropertyNameAttr.ConstructorArguments.Length > 0)
		{
			return jsonPropertyNameAttr.ConstructorArguments[0].Value?.ToString() ?? member.Name;
		}

		// Apply naming convention
		return ApplyNamingConvention(member.Name, naming);
	}

	private static string ApplyNamingConvention(string name, NamingConvention naming)
	{
		return naming switch
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
	}

	private static string ToCamelCase(string name)
	{
		if (string.IsNullOrEmpty(name) || char.IsLower(name[0]))
			return name;
		return char.ToLowerInvariant(name[0]) + name.Substring(1);
	}

	private static string ToPascalCase(string name)
	{
		if (string.IsNullOrEmpty(name) || char.IsUpper(name[0]))
			return name;
		return char.ToUpperInvariant(name[0]) + name.Substring(1);
	}

	private static string ToSnakeCase(string name)
	{
		if (string.IsNullOrEmpty(name))
			return name;

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
			{
				result.Append(name[i]);
			}
		}

		return result.ToString();
	}

	private static string ToKebabCase(string name)
	{
		return ToSnakeCase(name).Replace('_', '-');
	}

	private static string GetSchemaPropertyName(INamedTypeSymbol typeSymbol)
	{
		// Handle nested types
		if (typeSymbol.ContainingType != null)
		{
			return $"{GetSchemaPropertyName(typeSymbol.ContainingType)}_{typeSymbol.Name}";
		}

		return typeSymbol.Name;
	}

	private static bool IsNullableType(ITypeSymbol typeSymbol)
	{
		// Check for Nullable<T>
		if (typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
			return true;

		// Check for nullable reference types
		return typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;
	}

	private static ITypeSymbol UnwrapNullable(ITypeSymbol typeSymbol)
	{
		if (typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
		    typeSymbol is INamedTypeSymbol namedType)
		{
			return namedType.TypeArguments[0];
		}
		return typeSymbol;
	}

	private static bool HasAttribute(ISymbol symbol, string attributeName)
	{
		return symbol.GetAttributes().Any(a => a.AttributeClass?.Name == attributeName);
	}

	private static string? GetXmlDocSummary(ISymbol symbol)
	{
		var xml = symbol.GetDocumentationCommentXml();
		if (string.IsNullOrWhiteSpace(xml))
			return null;

		// Simple extraction of <summary> text
		var summaryStart = xml.IndexOf("<summary>", StringComparison.Ordinal);
		var summaryEnd = xml.IndexOf("</summary>", StringComparison.Ordinal);

		if (summaryStart >= 0 && summaryEnd > summaryStart)
		{
			var summaryText = xml.Substring(summaryStart + 9, summaryEnd - summaryStart - 9);
			return summaryText.Trim();
		}

		return null;
	}
}
