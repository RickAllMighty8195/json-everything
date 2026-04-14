using Json.Schema.Keywords;

namespace Json.Schema.DataGeneration.Requirements;

internal class ContainsRequirementsGatherer : IRequirementsGatherer
{
	public void AddRequirements(RequirementsContext context, JsonSchemaNode schema)
	{
		var contains = schema.GetKeyword<ContainsKeyword>() ??
		               schema.GetKeyword<Keywords.Draft06.ContainsKeyword>();
		if (contains != null)
		{
			if (context.Contains != null)
				context.Contains.And(contains.Subschemas[0].GetRequirements(context.CreateBranchContext()));
			else
				context.Contains = contains.Subschemas[0].GetRequirements(context.CreateBranchContext());
		}

		var range = NumberRangeSet.Full;
		var minimum = schema.GetKeyword<MinContainsKeyword>()?.RawValue.GetInt64();
		if (minimum != null)
			range = range.Floor(minimum.Value);
		var maximum = schema.GetKeyword<MaxContainsKeyword>()?.RawValue.GetInt64();
		if (maximum != null)
			range = range.Ceiling(maximum.Value);
		if (range != NumberRangeSet.Full)
		{
			if (context.ContainsCounts != null)
				context.ContainsCounts *= range;
			else
				context.ContainsCounts = range;
		}
	}
}