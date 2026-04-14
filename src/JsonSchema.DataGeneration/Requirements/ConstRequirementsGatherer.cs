using Json.Schema.Keywords;
using Json.Pointer;

namespace Json.Schema.DataGeneration.Requirements;

internal class ConstRequirementsGatherer : IRequirementsGatherer
{
	public void AddRequirements(RequirementsContext context, JsonSchemaNode schema)
	{
		var constKeyword = schema.GetKeyword<ConstKeyword>()?.RawValue;
		#pragma warning disable CS0618 // Type or member is obsolete
		var constSource = schema.PathFromResourceRoot.Combine(JsonPointer.Create("const"));
		#pragma warning restore CS0618 // Type or member is obsolete
		if (constKeyword != null)
		{
			if (context.ConstIsSet)
				context.HasConflict = true;
			else
			{
				context.Const = constKeyword.Value;
				context.ConstIsSet = true;
				context.ConstSource = constSource;
			}
		}
	}
}