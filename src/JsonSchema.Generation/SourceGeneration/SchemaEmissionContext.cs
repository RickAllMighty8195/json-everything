using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Json.Schema.Generation.SourceGeneration.Emitters;

namespace Json.Schema.Generation.SourceGeneration;

internal class SchemaEmissionContext
{
	public Dictionary<string, (string DefName, ITypeSymbol Symbol)> TypeReferences { get; } = new();
	public Dictionary<string, string> TypeIds { get; } = new();
	private HashSet<string> HandlerTargets { get; } = new();
	private HashSet<string> OpenGenericHandlerTargets { get; } = new();
	public ITypeSymbol? RootType { get; set; }

	public SchemaEmissionContext(Dictionary<string, string>? typeIds = null, List<SchemaHandlerInfo>? schemaHandlers = null)
	{
		if (typeIds != null)
			TypeIds = typeIds;

		if (schemaHandlers == null) return;

		foreach (var handler in schemaHandlers)
		{
			if (handler.IsOpenGenericTarget)
				OpenGenericHandlerTargets.Add(handler.TargetTypeName);
			else
				HandlerTargets.Add(handler.TargetTypeName);
		}
	}

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

	public string GetRefUri(ITypeSymbol typeSymbol)
	{
		if (RootType != null && SymbolEqualityComparer.Default.Equals(
			CodeEmitterHelpers.UnwrapNullable(typeSymbol), 
			CodeEmitterHelpers.UnwrapNullable(RootType)))
		{
			return "#";
		}
		
		var typeKey = GetTypeKey(typeSymbol);
		return TypeIds.TryGetValue(typeKey, out var id) ? id : "";
	}

	public bool HasSchemaHandler(ITypeSymbol typeSymbol)
	{
		var unwrapped = CodeEmitterHelpers.UnwrapNullable(typeSymbol);
		var fullyQualified = unwrapped.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
		if (HandlerTargets.Contains(fullyQualified))
			return true;

		if (unwrapped is INamedTypeSymbol { IsGenericType: true } named)
		{
			var unbound = named.ConstructUnboundGenericType().ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
			return OpenGenericHandlerTargets.Contains(unbound);
		}

		return false;
	}
}
