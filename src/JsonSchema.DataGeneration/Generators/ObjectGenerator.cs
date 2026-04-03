using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Bogus;

namespace Json.Schema.DataGeneration.Generators;

internal class ObjectGenerator : IDataGenerator
{
	private static readonly Faker _faker = new();

	// TODO: move these to a public settings object
	public static uint DefaultMinProperties { get; set; } = 0;
	public static uint DefaultMaxProperties { get; set; } = 10;
	public static uint DefaultMinContains { get; set; } = 1;
	public static uint DefaultMaxContains { get; set; } = 10;

	public static ObjectGenerator Instance { get; } = new();
	public SchemaValueType Type => SchemaValueType.Object;

	private ObjectGenerator() { }

	public GenerationResult Generate(RequirementsContext context)
	{
		var propertyNameRequirements = context.PropertyNames;
		if (propertyNameRequirements?.HasConflict == true || propertyNameRequirements?.IsFalse == true)
			return GenerationResult.Fail("No valid property names possible");

		var minProperties = DefaultMinProperties;
		var maxProperties = DefaultMaxProperties;
		if (context.PropertyCounts != null)
		{
			if (!context.PropertyCounts.Ranges.Any()) return GenerationResult.Fail("No valid property counts possible");

			var numberRange = JsonSchemaExtensions.Randomizer.ArrayElement(context.PropertyCounts.Ranges.ToArray());
			if (numberRange.Minimum.Value != NumberRangeSet.MinRangeValue)
				minProperties = (uint)(numberRange.Minimum.Inclusive
					? numberRange.Minimum.Value
					: numberRange.Minimum.Value + 1);
			if (numberRange.Maximum.Value != NumberRangeSet.MaxRangeValue)
				maxProperties = (uint)(numberRange.Maximum.Inclusive
					? numberRange.Maximum.Value
					: numberRange.Maximum.Value - 1);
		}

		var propertyCount = (int)JsonSchemaExtensions.Randomizer.UInt(minProperties, maxProperties);
		var containsCount = 0;
		if (context.Contains != null)
		{
			var minContains = DefaultMinContains;
			var maxContains = Math.Min(maxProperties, DefaultMaxContains + minContains);
			if (context.ContainsCounts != null)
			{
				var numberRange = JsonSchemaExtensions.Randomizer.ArrayElement(context.ContainsCounts.Ranges.ToArray());
				if (numberRange.Minimum.Value != NumberRangeSet.MinRangeValue)
					minContains = (uint)numberRange.Minimum.Value;
				if (numberRange.Maximum.Value != NumberRangeSet.MaxRangeValue)
					maxContains = (uint)numberRange.Maximum.Value;
			}

			if (minContains > maxContains) return GenerationResult.Fail("minContains is greater than maxContains");
			if (minContains > maxProperties) return GenerationResult.Fail("minContains is greater than maxItems less property count");

			containsCount = (int)JsonSchemaExtensions.Randomizer.UInt(minContains, maxContains);
			if (propertyCount < containsCount)
				propertyCount = containsCount;
		}

		var propertyGenerationResults = new Dictionary<string, GenerationResult>();
		var definedPropertyNames = new List<string>();
		var remainingPropertyCount = propertyCount;
		if (context.RequiredProperties != null)
		{
			if (context.RequiredProperties.Any(x => !SatisfiesPropertyNameRequirements(x, propertyNameRequirements)))
				return GenerationResult.Fail("Required property names do not satisfy propertyNames constraints");

			definedPropertyNames.AddRange(context.RequiredProperties);
			remainingPropertyCount -= context.RequiredProperties.Count;
		}

		if (context.AvoidProperties != null)
		{
			foreach (var avoidProperty in context.AvoidProperties)
			{
				if (definedPropertyNames.Remove(avoidProperty))
					remainingPropertyCount++;
			}
		}

		if (context.Properties != null)
		{
			var propertyNames = context.Properties
				.Where(x => !definedPropertyNames.Contains(x.Key) && !x.Value.IsFalse && !x.Value.HasConflict && SatisfiesPropertyNameRequirements(x.Key, propertyNameRequirements))
				.Select(x => x.Key)
				.ToArray();
			if (propertyNames.Length != 0)
			{
				propertyNames = JsonSchemaExtensions.Randomizer.ArrayElements(propertyNames, Math.Min(propertyNames.Length, remainingPropertyCount));
				definedPropertyNames.AddRange(propertyNames);
				remainingPropertyCount -= propertyNames.Length;
			}
		}

		var remainingProperties = context.RemainingProperties ?? new RequirementsContext();
		if (remainingProperties.IsFalse)
			remainingPropertyCount = 0;
		remainingPropertyCount = Math.Max(0, remainingPropertyCount);

		var usedPropertyNames = new HashSet<string>(definedPropertyNames);
		var otherPropertyNames = GenerateAdditionalPropertyNames(remainingPropertyCount, usedPropertyNames, propertyNameRequirements);
		if (otherPropertyNames.Length != remainingPropertyCount)
			return GenerationResult.Fail("Could not generate enough property names that satisfy propertyNames constraints");

		var allPropertyNames = definedPropertyNames.Concat(otherPropertyNames).ToArray();
		var containsProperties = JsonSchemaExtensions.Randomizer
			.ArrayElements(allPropertyNames, Math.Min(allPropertyNames.Length, containsCount))
			.ToHashSet();

		foreach (var propertyName in allPropertyNames)
		{
			if (context.Properties?.TryGetValue(propertyName, out var propertyRequirement) != true)
				propertyRequirement = context.RemainingProperties ?? new RequirementsContext();

			if (containsCount > 0 && containsProperties.Contains(propertyName))
			{
				propertyRequirement = new RequirementsContext(propertyRequirement!);
				propertyRequirement.And(context.Contains!);
			}

			var propertyGenerationResult = propertyRequirement!.GenerateData();
			if (!propertyGenerationResult.IsSuccess)
				return GenerationResult.Fail(new[] { propertyGenerationResult });

			propertyGenerationResults[propertyName] = propertyGenerationResult;
		}

		return propertyGenerationResults.All(x => x.Value.IsSuccess)
			? GenerationResult.Success(new JsonObject(propertyGenerationResults.ToDictionary(x => x.Key, x => x.Value.Result?.DeepClone())))
			: GenerationResult.Fail(propertyGenerationResults.Values);
	}

	private static bool SatisfiesPropertyNameRequirements(string propertyName, RequirementsContext? propertyNameRequirements)
	{
		if (propertyNameRequirements == null) return true;
		if (propertyNameRequirements.IsFalse || propertyNameRequirements.HasConflict) return false;
		if (propertyNameRequirements.Type.HasValue &&
		    !propertyNameRequirements.Type.Value.HasFlag(SchemaValueType.String))
			return false;
		if (propertyNameRequirements.StringLengths != null &&
		    propertyNameRequirements.StringLengths.Ranges.Any() &&
		    !propertyNameRequirements.StringLengths.Ranges.Any(x => x.Contains(propertyName.Length)))
			return false;

		if (propertyNameRequirements.Patterns != null)
		{
			foreach (var pattern in propertyNameRequirements.Patterns)
			{
				try
				{
					if (!Regex.IsMatch(propertyName, pattern)) return false;
				}
				catch (ArgumentException)
				{
					return false;
				}
			}
		}

		if (propertyNameRequirements.AntiPatterns != null)
		{
			foreach (var antiPattern in propertyNameRequirements.AntiPatterns)
			{
				try
				{
					if (Regex.IsMatch(propertyName, antiPattern)) return false;
				}
				catch (ArgumentException)
				{
					return false;
				}
			}
		}

		return true;
	}

	private static string[] GenerateAdditionalPropertyNames(int count, HashSet<string> usedPropertyNames, RequirementsContext? propertyNameRequirements)
	{
		if (count <= 0) return [];

		if (propertyNameRequirements == null)
		{
			return _faker.Lorem.Sentence(count * 2).Split(' ')
				.Distinct()
				.Where(x => !usedPropertyNames.Contains(x))
				.Take(count)
				.ToArray();
		}

		var generatedNames = new List<string>(count);
		for (var i = 0; i < 100 && generatedNames.Count < count; i++)
		{
			var nameRequirements = new RequirementsContext(propertyNameRequirements);
			nameRequirements.InferredType |= SchemaValueType.String;
			var generationResult = nameRequirements.GenerateData();
			if (!generationResult.IsSuccess || generationResult.Result == null) continue;

			string? candidate;
			try
			{
				candidate = generationResult.Result.GetValue<string>();
			}
			catch
			{
				continue;
			}

			if (string.IsNullOrEmpty(candidate) || usedPropertyNames.Contains(candidate)) continue;

			usedPropertyNames.Add(candidate);
			generatedNames.Add(candidate);
		}

		return generatedNames.ToArray();
	}
}