using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Json.Schema.Generation.SourceGeneration.Emitters;

namespace Json.Schema.Generation.SourceGeneration;

internal class SchemaEmissionContext
{
	public Dictionary<string, (string DefName, ITypeSymbol Symbol)> TypeReferences { get; } = new();

	public static string GetTypeKey(ITypeSymbol typeSymbol)
	{
		var unwrapped = CodeEmitterHelpers.UnwrapNullable(typeSymbol);
		return unwrapped.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
	}

	public static string GetDefinitionName(ITypeSymbol typeSymbol)
	{
		var unwrapped = CodeEmitterHelpers.UnwrapNullable(typeSymbol);
		var displayName = unwrapped.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
		
		var lastDot = displayName.LastIndexOf('.');
		var simpleName = lastDot >= 0 ? displayName[(lastDot + 1)..] : displayName;
		
		var backtickIndex = simpleName.IndexOf('`');
		if (backtickIndex >= 0)
			simpleName = simpleName[..backtickIndex];
		
		simpleName = simpleName
			.Replace("<", "_")
			.Replace(">", "")
			.Replace(",", "_")
			.Replace("?", "")
			.Replace(" ", "");
		
		return simpleName;
	}

	public bool ShouldUseRef(ITypeSymbol typeSymbol)
	{
		var typeKey = GetTypeKey(typeSymbol);
		return TypeReferences.ContainsKey(typeKey);
	}

	public string GetRefUri(ITypeSymbol typeSymbol)
	{
		var typeKey = GetTypeKey(typeSymbol);
		return TypeReferences.TryGetValue(typeKey, out var info)
			? $"#/$defs/{info.DefName}"
			: "";
	}
}
