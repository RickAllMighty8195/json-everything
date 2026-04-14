using System;
using Json.Schema.Keywords;

namespace Json.Schema.DataGeneration.Requirements;

internal class RefRequirementsGatherer : IRequirementsGatherer
{
	private const SchemaValueType _primitiveTypes = SchemaValueType.Null | SchemaValueType.String | SchemaValueType.Integer | SchemaValueType.Number | SchemaValueType.Boolean;

	public void AddRequirements(RequirementsContext context, JsonSchemaNode schema)
	{
		var refKeyword = schema.GetKeyword<RefKeyword>();
		if (refKeyword == null) return;
		if (refKeyword.Subschemas is null or { Length: 0 })
			throw new RefResolutionException((Uri)refKeyword.Value!);

		if (context.RemainingRefDepth > 0)
		{
			var refRequirements = refKeyword.Subschemas[0].GetRequirements(context.CreateRefBranchContext());
			context.And(refRequirements);
			return;
		}

		var fallbackRequirements = BuildCutoffFallbackRequirements(refKeyword.Subschemas[0]);
		context.And(fallbackRequirements);
	}

	private static RequirementsContext BuildCutoffFallbackRequirements(JsonSchemaNode target)
	{
		var fallback = new RequirementsContext
		{
			ReachedRefDepthCutoff = true
		};

		var typeKeyword = target.GetKeyword<TypeKeyword>();
		if (typeKeyword == null)
		{
			fallback.IsFalse = true;
			return fallback;
		}

		var allowedTypes = (SchemaValueType)typeKeyword.Value!;
		var primitiveTypes = allowedTypes & _primitiveTypes;
		if (primitiveTypes == 0)
		{
			fallback.IsFalse = true;
			return fallback;
		}

		fallback.Type = primitiveTypes.HasFlag(SchemaValueType.Null)
			? SchemaValueType.Null
			: primitiveTypes.HasFlag(SchemaValueType.String)
				? SchemaValueType.String
				: primitiveTypes.HasFlag(SchemaValueType.Integer)
					? SchemaValueType.Integer
					: primitiveTypes.HasFlag(SchemaValueType.Number)
						? SchemaValueType.Number
						: SchemaValueType.Boolean;

		return fallback;
	}
}