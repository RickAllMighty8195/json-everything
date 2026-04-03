using Json.Schema.Keywords;
using Json.Pointer;

namespace Json.Schema.DataGeneration.Requirements;

internal class NumberRequirementsGatherer : IRequirementsGatherer
{
	public void AddRequirements(RequirementsContext context, JsonSchemaNode schema, BuildOptions options)
	{
		var supportsNumbers = false;
		JsonPointer? rangeSource = null;

		#pragma warning disable CS0618 // Type or member is obsolete
		var baseSource = schema.PathFromResourceRoot;
		#pragma warning restore CS0618 // Type or member is obsolete

		var range = NumberRangeSet.Full;
		var minimum = schema.GetKeyword<MinimumKeyword>()?.RawValue.GetDecimal();
		if (minimum != null)
		{
			range = range.Floor(minimum.Value);
			rangeSource ??= baseSource.Combine(JsonPointer.Create("minimum"));
			supportsNumbers = true;
		}
		minimum = schema.GetKeyword<ExclusiveMinimumKeyword>()?.RawValue.GetDecimal();
		if (minimum != null)
		{
			range = range.Floor((minimum.Value, false));
			rangeSource ??= baseSource.Combine(JsonPointer.Create("exclusiveMinimum"));
			supportsNumbers = true;
		}
		var maximum = schema.GetKeyword<MaximumKeyword>()?.RawValue.GetDecimal();
		if (maximum != null)
		{
			range = range.Ceiling(maximum.Value);
			rangeSource ??= baseSource.Combine(JsonPointer.Create("maximum"));
			supportsNumbers = true;
		}
		maximum = schema.GetKeyword<ExclusiveMaximumKeyword>()?.RawValue.GetDecimal();
		if (maximum != null)
		{
			range = range.Ceiling((maximum.Value, false));
			rangeSource ??= baseSource.Combine(JsonPointer.Create("exclusiveMaximum"));
			supportsNumbers = true;
		}
		if (range != NumberRangeSet.Full)
		{
			if (context.NumberRanges != null)
				context.NumberRanges *= range;
			else
			{
				context.NumberRanges = range;
				context.NumberRangesSource = rangeSource;
			}
			context.NumberRangesSource ??= rangeSource;
			supportsNumbers = true;
		}

		var multipleOf = schema.GetKeyword<MultipleOfKeyword>()?.RawValue.GetDecimal();
		if (multipleOf != null)
		{
			if (context.Multiples != null)
				context.Multiples?.Add(multipleOf.Value);
			else
				context.Multiples = [multipleOf.Value];
			supportsNumbers = true;
		}

		if (supportsNumbers)
			context.InferredType |= SchemaValueType.Number;
	}
}