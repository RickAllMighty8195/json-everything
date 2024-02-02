﻿using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Logic.Rules;

namespace Json.Logic;

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
	public static JsonSerializerOptions WithJsonLogic(this JsonSerializerOptions options)
	{
		options.TypeInfoResolverChain.Add(JsonLogicSerializerContext.Default);
		return options;
	}
}

[JsonSerializable(typeof(AddRule))]
[JsonSerializable(typeof(AllRule))]
[JsonSerializable(typeof(AndRule))]
[JsonSerializable(typeof(BooleanCastRule))]
[JsonSerializable(typeof(CatRule))]
[JsonSerializable(typeof(DivideRule))]
[JsonSerializable(typeof(FilterRule))]
[JsonSerializable(typeof(IfRule))]
[JsonSerializable(typeof(InRule))]
[JsonSerializable(typeof(LessThanEqualRule))]
[JsonSerializable(typeof(LessThanRule))]
[JsonSerializable(typeof(LiteralRule))]
[JsonSerializable(typeof(LogRule))]
[JsonSerializable(typeof(LooseEqualsRule))]
[JsonSerializable(typeof(LooseNotEqualsRule))]
[JsonSerializable(typeof(MapRule))]
[JsonSerializable(typeof(MaxRule))]
[JsonSerializable(typeof(MergeRule))]
[JsonSerializable(typeof(MinRule))]
[JsonSerializable(typeof(MissingRule))]
[JsonSerializable(typeof(MissingSomeRule))]
[JsonSerializable(typeof(ModRule))]
[JsonSerializable(typeof(MoreThanEqualRule))]
[JsonSerializable(typeof(MoreThanRule))]
[JsonSerializable(typeof(MultiplyRule))]
[JsonSerializable(typeof(NoneRule))]
[JsonSerializable(typeof(NotRule))]
[JsonSerializable(typeof(OrRule))]
[JsonSerializable(typeof(ReduceRule))]
[JsonSerializable(typeof(RuleCollection))]
[JsonSerializable(typeof(SomeRule))]
[JsonSerializable(typeof(StrictEqualsRule))]
[JsonSerializable(typeof(StrictNotEqualsRule))]
[JsonSerializable(typeof(SubstrRule))]
[JsonSerializable(typeof(SubtractRule))]
[JsonSerializable(typeof(VariableRule))]
[JsonSerializable(typeof(JsonNode))]
[JsonSerializable(typeof(Rule[]))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string))]
internal partial class JsonLogicSerializerContext : JsonSerializerContext;
