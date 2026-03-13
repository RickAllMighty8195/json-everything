using System;
using Json.Schema.Keywords;

namespace Json.Schema.DataGeneration.Requirements;

internal class RefRequirementsGatherer : IRequirementsGatherer
{
	public void AddRequirements(RequirementsContext context, JsonSchemaNode schema, BuildOptions options)
	{
		var refKeyword = schema.GetKeyword<RefKeyword>();
		if (refKeyword != null)
		{
			if (refKeyword.Subschemas is null or { Length: 0 })
				throw new RefResolutionException((Uri)refKeyword.Value!);

			context.And(refKeyword.Subschemas[0].GetRequirements(options));
		}
	}
}