﻿using System.Text.Json;
using System.Text.Json.Serialization;

namespace Json.Path;

/// <summary>
/// Extension methods for <see cref="JsonSerializerOptions"/>.
/// </summary>
public static class JsonSerializerOptionsExtensions
{
	/// <summary>
	/// Adds serializer context information to the type resolver chain.
	/// </summary>
	/// <param name="options">The options.</param>
	/// <returns>The same options.</returns>
	public static JsonSerializerOptions WithJsonPath(this JsonSerializerOptions options)
	{
		options.TypeInfoResolverChain.Add(JsonPathSerializerContext.Default);
		return options;
	}
}

[JsonSerializable(typeof(JsonPath))]
internal partial class JsonPathSerializerContext : JsonSerializerContext;