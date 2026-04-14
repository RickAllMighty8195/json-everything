using System;
using Json.Schema.Keywords;

namespace Json.Schema.DataGeneration.Requirements;

internal class PropertiesRequirementsGatherer : IRequirementsGatherer
{
	public void AddRequirements(RequirementsContext context, JsonSchemaNode schema)
	{
		var supportsObjects = false;

		var range = NumberRangeSet.Full;
		var minimum = schema.GetKeyword<MinPropertiesKeyword>()?.RawValue.GetInt64();
		if (minimum != null)
		{
			range = range.Floor(minimum.Value);
			supportsObjects = true;
		}
		var maximum = schema.GetKeyword<MaxPropertiesKeyword>()?.RawValue.GetInt64();
		if (maximum != null)
		{
			range = range.Ceiling(maximum.Value);
			supportsObjects = true;
		}
		if (range != NumberRangeSet.Full)
		{
			if (context.PropertyCounts != null)
				context.PropertyCounts *= range;
			else
				context.PropertyCounts = range;
			supportsObjects = true;
		}

		var requiredProperties = (string[]?)schema.GetKeyword<RequiredKeyword>()?.Value;
		if (requiredProperties != null)
		{
			if (context.RequiredProperties != null)
				context.RequiredProperties.AddRange(requiredProperties);
			else
				context.RequiredProperties = [.. requiredProperties];
			supportsObjects = true;
		}

		var properties = schema.GetKeyword<PropertiesKeyword>();
		if (properties != null)
		{
			context.Properties ??= [];
			foreach (var property in properties.Subschemas)
			{
				var propertyName = property.RelativePath[0].ToString();
				var propertyRequirements = property.GetRequirements(context.CreateBranchContext());
				var isRequired = requiredProperties != null && Array.IndexOf(requiredProperties, propertyName) >= 0;
				if (!isRequired && propertyRequirements.ReachedRefDepthCutoff)
					propertyRequirements.IsFalse = true;

				if (context.Properties.TryGetValue(propertyName, out var subschema))
					subschema.And(propertyRequirements);
				else
					context.Properties.Add(propertyName, propertyRequirements);
			}
			supportsObjects = true;
		}

		var additionalProperties = schema.GetKeyword<AdditionalPropertiesKeyword>();
		if (additionalProperties != null)
		{
			if (context.RemainingProperties != null)
				context.RemainingProperties.And(additionalProperties.Subschemas[0].GetRequirements(context.CreateBranchContext()));
			else
				context.RemainingProperties = additionalProperties.Subschemas[0].GetRequirements(context.CreateBranchContext());
			supportsObjects = true;
		}

		var propertyNames = schema.GetKeyword<PropertyNamesKeyword>();
		if (propertyNames != null)
		{
			if (context.PropertyNames != null)
				context.PropertyNames.And(propertyNames.Subschemas[0].GetRequirements(context.CreateBranchContext()));
			else
				context.PropertyNames = propertyNames.Subschemas[0].GetRequirements(context.CreateBranchContext());
			supportsObjects = true;
		}

		additionalProperties = schema.GetKeyword<UnevaluatedPropertiesKeyword>();
		if (additionalProperties != null)
		{
			if (context.RemainingProperties != null)
				context.RemainingProperties.And(additionalProperties.Subschemas[0].GetRequirements(context.CreateBranchContext()));
			else
				context.RemainingProperties = additionalProperties.Subschemas[0].GetRequirements(context.CreateBranchContext());
			supportsObjects = true;
		}

		if (supportsObjects)
			context.InferredType |= SchemaValueType.Object;
	}
}