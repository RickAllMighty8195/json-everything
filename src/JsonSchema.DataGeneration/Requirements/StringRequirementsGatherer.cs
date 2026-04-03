using System.Text.RegularExpressions;
using Json.Schema.Keywords;
using Json.Pointer;

namespace Json.Schema.DataGeneration.Requirements;

internal class StringRequirementsGatherer : IRequirementsGatherer
{
	public void AddRequirements(RequirementsContext context, JsonSchemaNode schema, BuildOptions options)
	{
		var supportsStrings = false;

		var range = NumberRangeSet.NonNegative;
		JsonPointer? stringLengthsSource = null;

		#pragma warning disable CS0618 // Type or member is obsolete
		var baseSource = schema.PathFromResourceRoot;
		#pragma warning restore CS0618 // Type or member is obsolete

		var minLength = schema.GetKeyword<MinLengthKeyword>()?.RawValue.GetDecimal();
		if (minLength != null)
		{
			range = range.Floor(minLength.Value);
			stringLengthsSource ??= baseSource.Combine(JsonPointer.Create("minLength"));
			supportsStrings = true;
		}
		var maxLength = schema.GetKeyword<MaxLengthKeyword>()?.RawValue.GetDecimal();
		if (maxLength != null)
		{
			range = range.Ceiling(maxLength.Value);
			stringLengthsSource ??= baseSource.Combine(JsonPointer.Create("maxLength"));
			supportsStrings = true;
		}
		if (range != NumberRangeSet.NonNegative)
		{
			if (context.StringLengths != null)
				context.StringLengths *= range;
			else
			{
				context.StringLengths = range;
				context.StringLengthsSource = stringLengthsSource;
			}
			supportsStrings = true;
		}

		var pattern = (Regex?)schema.GetKeyword<PatternKeyword>()?.Value;
		if (pattern != null)
		{
			context.Patterns ??= [];
			context.Patterns.Add(pattern.ToString());
			supportsStrings = true;
		}

		if (context.Format == null)
		{
			context.Format = schema.GetKeyword<FormatKeyword>()?.RawValue.GetString() ??
			                 schema.GetKeyword<Keywords.Draft06.FormatKeyword>()?.RawValue.GetString();
			if (context.Format != null)
			{
				#pragma warning disable CS0618 // Type or member is obsolete
				context.FormatSource = schema.PathFromResourceRoot.Combine(JsonPointer.Create("format"));
				#pragma warning restore CS0618 // Type or member is obsolete
			}
			supportsStrings = context.Format != null;
		}

		if (supportsStrings)
			context.InferredType |= SchemaValueType.String;
	}
}