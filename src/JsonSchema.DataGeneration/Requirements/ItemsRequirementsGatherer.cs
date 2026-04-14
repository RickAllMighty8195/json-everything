using System.Linq;
using System.Text.Json;
using Json.Schema.Keywords;
using Json.Schema.Keywords.Draft06;
using ItemsKeyword = Json.Schema.Keywords.ItemsKeyword;

namespace Json.Schema.DataGeneration.Requirements;

internal class ItemsRequirementsGatherer : IRequirementsGatherer
{
	public void AddRequirements(RequirementsContext context, JsonSchemaNode schema)
	{
		var supportsArrays = false;

		var range = NumberRangeSet.Full;
		var minimum = schema.GetKeyword<MinItemsKeyword>()?.RawValue.GetInt64();
		if (minimum != null)
		{
			range = range.Floor(minimum.Value);
			supportsArrays = true;
		}
		var maximum = schema.GetKeyword<MaxItemsKeyword>()?.RawValue.GetInt64();
		if (maximum != null)
		{
			range = range.Ceiling(maximum.Value);
			supportsArrays = true;
		}
		if (range != NumberRangeSet.Full)
		{
			if (context.ItemCounts != null)
				context.ItemCounts *= range;
			else
				context.ItemCounts = range;
		}

		var items = schema.GetKeyword<ItemsKeyword>();
		if (items != null)
		{
			if (items.RawValue.ValueKind is JsonValueKind.Object or JsonValueKind.True or JsonValueKind.False)
			{
				if (context.RemainingItems != null)
					context.RemainingItems.And(items.Subschemas[0].GetRequirements(context.CreateBranchContext()));
				else
					context.RemainingItems = items.Subschemas[0].GetRequirements(context.CreateBranchContext());
			}
			else
			{
				var incomingSequentialItems = items.Subschemas.Select(x => x.GetRequirements(context.CreateBranchContext())).ToList();
				if (context.SequentialItems != null)
					context.And(new RequirementsContext { SequentialItems = incomingSequentialItems });
				else
					context.SequentialItems = incomingSequentialItems;
			}
			supportsArrays = true;
		}

		var prefixItems = schema.GetKeyword<PrefixItemsKeyword>();
		if (prefixItems != null)
		{
			var incomingSequentialItems = prefixItems.Subschemas.Select(x => x.GetRequirements(context.CreateBranchContext())).ToList();
			if (context.SequentialItems != null)
				context.And(new RequirementsContext { SequentialItems = incomingSequentialItems });
			else
				context.SequentialItems = incomingSequentialItems;
			supportsArrays = true;
		}

		var additionalItems = schema.GetKeyword<AdditionalItemsKeyword>();
		if (additionalItems != null)
		{
			if (context.RemainingItems != null)
				context.RemainingItems.And(additionalItems.Subschemas[0].GetRequirements(context.CreateBranchContext()));
			else
				context.RemainingItems = additionalItems.Subschemas[0].GetRequirements(context.CreateBranchContext());
			supportsArrays = true;
		}

		additionalItems = schema.GetKeyword<UnevaluatedItemsKeyword>();
		if (additionalItems != null)
		{
			if (context.RemainingItems != null)
				context.RemainingItems.And(additionalItems.Subschemas[0].GetRequirements(context.CreateBranchContext()));
			else
				context.RemainingItems = additionalItems.Subschemas[0].GetRequirements(context.CreateBranchContext());
			supportsArrays = true;
		}

		if (supportsArrays)
			context.InferredType |= SchemaValueType.Array;
	}
}