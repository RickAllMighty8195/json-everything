using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using Json.Pointer;
using Json.Schema.DataGeneration.Generators;
using Json.Schema.DataGeneration.Requirements;

namespace Json.Schema.DataGeneration;

/// <summary>
/// Provides extension methods for <see cref="JsonSchema"/> to generate sample data.
/// </summary>
public static class JsonSchemaExtensions
{
	private static readonly IPrioritizedDataGenerator[] _priorityGenerators =
	{
		ConstGenerator.Instance,
		EnumGenerator.Instance
	};
	// simplest weighting is just to duplicate entries
	private static readonly IDataGenerator[] _generators =
	{
		ObjectGenerator.Instance,
		ArrayGenerator.Instance,
		IntegerGenerator.Instance,
		IntegerGenerator.Instance,
		NumberGenerator.Instance,
		NumberGenerator.Instance,
		StringGenerator.Instance,
		StringGenerator.Instance,
		BooleanGenerator.Instance,
		BooleanGenerator.Instance,
		NullGenerator.Instance,
		NullGenerator.Instance,
	};

	internal static readonly Randomizer Randomizer = new();

	/// <summary>
	/// Attempts to generate sample data that meets the requirements of the schema.
	/// </summary>
	/// <param name="schema">The schema.</param>
	/// <param name="options">not used</param>
	/// <returns>A result object indicating success and containing the result or error message.</returns>
	[Obsolete("Options is no longer used.  Parameter will be removed at next major version.")]
	public static GenerationResult GenerateData(this JsonSchema schema, BuildOptions? options) => schema.GenerateData();

	/// <summary>
	/// Attempts to generate sample data that meets the requirements of the schema.
	/// </summary>
	/// <param name="schema">The schema.</param>
	/// <returns>A result object indicating success and containing the result or error message.</returns>
	public static GenerationResult GenerateData(this JsonSchema schema)
	{
		var requirements = schema.Root.GetRequirements(new RequirementsContext { RemainingRefDepth = RequirementsContext.RecursiveRefHardStop });

		return requirements.GenerateData();
	}

	internal static GenerationResult GenerateData(this RequirementsContext requirements)
	{
		if (requirements.IsFalse)
			return GenerationResult.Fail("Boolean schema `false` allows no values");

		var failures = new List<GenerationResult>();
		var conflictVariations = 0;
		var noGeneratorVariations = 0;
		foreach (var variation in Randomizer.Shuffle(requirements.GetAllVariations()))
		{
			if (variation.HasConflict)
			{
				conflictVariations++;
				if (failures.Count < 5)
				{
					if (variation.ErrorReasons != null && variation.ErrorReasons.Count != 0)
					{
						foreach (var reason in variation.ErrorReasons)
						{
							if (failures.Count >= 5) break;
							var schemaLocations = new List<JsonPointer>(2);
							if (reason.LeftSchemaLocation.HasValue)
								schemaLocations.Add(reason.LeftSchemaLocation.Value);
							if (reason.RightSchemaLocation.HasValue)
								schemaLocations.Add(reason.RightSchemaLocation.Value);
							failures.Add(GenerationResult.Fail(reason.Message, schemaLocations: schemaLocations.Count != 0 ? schemaLocations : null));
						}
					}
					else
						failures.Add(GenerationResult.Fail("Conflicting constraints were detected while combining schema requirements."));
				}
				continue;
			}

			var priorityGenerator = _priorityGenerators.FirstOrDefault(x => x.Applies(variation));
			if (priorityGenerator != null)
				return priorityGenerator.Generate(variation);

			var applicableGenerators = _generators
				.Where(x =>
				{
					if (variation.Type.HasValue) return variation.Type.Value.HasFlag(x.Type);
					if (variation.InferredType == default) return true;
					return variation.InferredType.HasFlag(x.Type);
				})
				.ToArray();
			if (applicableGenerators.Length == 0)
			{
				noGeneratorVariations++;
				continue;
			}

			var generator = Randomizer.ArrayElement(applicableGenerators);
			var result = generator.Generate(variation);
			if (result.IsSuccess) return result;
			if (failures.Count < 5)
				failures.Add(result);
		}

		return failures.Count > 0
			? GenerationResult.Fail(failures)
			: GenerationResult.Fail($"Could not generate data that validates against the schema. Tried {conflictVariations + noGeneratorVariations} variation(s): {conflictVariations} conflicting variation(s), {noGeneratorVariations} variation(s) had no applicable generator.");
	}

	private static readonly IRequirementsGatherer[] _requirementsGatherers =
	[
		new AllOfRequirementsGatherer(),
				new AnyOfRequirementsGatherer(),
				new ConditionalRequirementsGatherer(),
				new ConstRequirementsGatherer(),
				new ContainsRequirementsGatherer(),
				new EnumRequirementsGatherer(),
				new FalseRequirementsGatherer(),
				new ItemsRequirementsGatherer(),
				new NotRequirementsGatherer(),
				new NumberRequirementsGatherer(),
				new OneOfRequirementsGatherer(),
				new PropertiesRequirementsGatherer(),
				new RefRequirementsGatherer(),
				new StringRequirementsGatherer(),
				new TypeRequirementsGatherer()
	];

	internal static RequirementsContext GetRequirements(this JsonSchemaNode schema, RequirementsContext? context = null)
	{
		context ??= new RequirementsContext { RemainingRefDepth = RequirementsContext.RecursiveRefHardStop };
		foreach (var gatherer in _requirementsGatherers)
		{
			gatherer.AddRequirements(context, schema);
		}

		return context;
	}
}