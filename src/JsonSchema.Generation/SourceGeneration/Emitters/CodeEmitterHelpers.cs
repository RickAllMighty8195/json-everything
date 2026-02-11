using Microsoft.CodeAnalysis;

namespace Json.Schema.Generation.SourceGeneration.Emitters;

/// <summary>
/// Helper methods for code emission.
/// </summary>
internal static class CodeEmitterHelpers
{
	public static string FormatValue(object? value)
	{
		return value switch
		{
			null => "null",
			string s => $"\"{EscapeString(s)}\"",
			bool b => b ? "true" : "false",
			decimal d => d.ToString(System.Globalization.CultureInfo.InvariantCulture) + "m",
			double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture) + "m",
			float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture) + "m",
			_ => value.ToString() ?? "null"
		};
	}

	public static string EscapeString(string str)
	{
		return str
			.Replace("\\", "\\\\")
			.Replace("\"", "\\\"")
			.Replace("\n", "\\n")
			.Replace("\r", "\\r")
			.Replace("\t", "\\t");
	}

	public static string EscapeXmlDoc(string str)
	{
		return str
			.Replace("&", "&amp;")
			.Replace("<", "&lt;")
			.Replace(">", "&gt;");
	}

	public static ITypeSymbol UnwrapNullable(ITypeSymbol typeSymbol)
	{
		if (typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
		    typeSymbol is INamedTypeSymbol namedType)
		{
			return namedType.TypeArguments[0];
		}
		return typeSymbol;
	}

	public static bool IsCollectionType(ITypeSymbol typeSymbol)
	{
		if (typeSymbol is not INamedTypeSymbol { IsGenericType: true } namedType) return false;

		var typeString = namedType.ConstructedFrom.ToDisplayString();
		return typeString is
			"System.Collections.Generic.List<T>" or
			"System.Collections.Generic.IList<T>" or
			"System.Collections.Generic.ICollection<T>" or
			"System.Collections.Generic.IEnumerable<T>" or
			"System.Collections.Generic.IReadOnlyList<T>" or
			"System.Collections.Generic.IReadOnlyCollection<T>";
	}

	public static ITypeSymbol? GetElementType(ITypeSymbol typeSymbol)
	{
		// Handle arrays
		if (typeSymbol is IArrayTypeSymbol arrayType)
		{
			return arrayType.ElementType;
		}

		// Handle generic collections
		if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
		{
			var typeString = namedType.ConstructedFrom.ToDisplayString();
			if (typeString is
			    "System.Collections.Generic.List<T>" or
			    "System.Collections.Generic.IList<T>" or
			    "System.Collections.Generic.ICollection<T>" or
			    "System.Collections.Generic.IEnumerable<T>" or
			    "System.Collections.Generic.IReadOnlyList<T>" or
			    "System.Collections.Generic.IReadOnlyCollection<T>")
			{
				return namedType.TypeArguments[0];
			}
		}

		return null;
	}
}
