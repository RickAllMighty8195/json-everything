#if NETSTANDARD2_0

using System.Collections.Generic;

namespace Json.Schema.DataGeneration;

internal static class DotnetCompatibility
{
	public static HashSet<TItem> ToHashSet<TItem>(this IEnumerable<TItem> items)
	{
		return new HashSet<TItem>(items);
	}
}

#endif