using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Json.Schema.Generation.SourceGeneration.Emitters;

namespace Json.Schema.Generation.SourceGeneration;

/// <summary>
/// Tracks which types should use $ref in the current schema being generated.
/// </summary>
internal class SchemaEmissionContext
{
	/// <summary>
	/// Types that should be referenced via $ref instead of inlined.
	/// Key is the type key (fully qualified name), value is (definition name, type symbol).
	/// </summary>
	public Dictionary<string, (string DefName, ITypeSymbol Symbol)> TypeReferences { get; } = new();

	/// <summary>
	/// Gets the type key for caching/reference purposes.
	/// </summary>
	public static string GetTypeKey(ITypeSymbol typeSymbol)
	{
		var unwrapped = CodeEmitterHelpers.UnwrapNullable(typeSymbol);
		return unwrapped.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
	}

	/// <summary>
	/// Gets the definition name for a type (used in $defs and $ref).
	/// </summary>
	public static string GetDefinitionName(ITypeSymbol typeSymbol)
	{
		var unwrapped = CodeEmitterHelpers.UnwrapNullable(typeSymbol);
		var displayName = unwrapped.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
		
		// Strip namespace and make it safe for JSON Schema
		var lastDot = displayName.LastIndexOf('.');
		var simpleName = lastDot >= 0 ? displayName.Substring(lastDot + 1) : displayName;
		
		// Remove generic markers like `1, `2
		var backtickIndex = simpleName.IndexOf('`');
		if (backtickIndex >= 0)
			simpleName = simpleName.Substring(0, backtickIndex);
		
		// Replace < > , ? with safe characters (? shouldn't appear but just in case)
		simpleName = simpleName
			.Replace("<", "_")
			.Replace(">", "")
			.Replace(",", "_")
			.Replace("?", "")
			.Replace(" ", "");
		
		return simpleName;
	}

	/// <summary>
	/// Checks if a type should use $ref.
	/// </summary>
	public bool ShouldUseRef(ITypeSymbol typeSymbol)
	{
		var typeKey = GetTypeKey(typeSymbol);
		return TypeReferences.ContainsKey(typeKey);
	}

	/// <summary>
	/// Gets the $ref URI for a type.
	/// </summary>
	public string GetRefUri(ITypeSymbol typeSymbol)
	{
		var typeKey = GetTypeKey(typeSymbol);
		if (TypeReferences.TryGetValue(typeKey, out var info))
		{
			return $"#/$defs/{info.DefName}";
		}
		return "";
	}
}
