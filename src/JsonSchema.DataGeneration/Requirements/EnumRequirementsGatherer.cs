using System.Linq;
using Json.Schema.Keywords;
using Json.Pointer;

namespace Json.Schema.DataGeneration.Requirements;

internal class EnumRequirementsGatherer : IRequirementsGatherer
{
	public void AddRequirements(RequirementsContext context, JsonSchemaNode schema, BuildOptions options)
	{
		var enumKeyword = schema.GetKeyword<EnumKeyword>();
		#pragma warning disable CS0618 // Type or member is obsolete
		var enumSource = schema.PathFromResourceRoot.Combine(JsonPointer.Create("enum"));
		#pragma warning restore CS0618 // Type or member is obsolete
		if (enumKeyword != null)
		{
			if (context.EnumOptions != null)
				context.HasConflict = true;
			else
			{
				context.EnumOptions = enumKeyword.RawValue.EnumerateArray().Select(x => (true, x)).ToList();
				context.EnumSource = enumSource;
			}
		}
	}
}