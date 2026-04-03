using Json.Schema.Keywords;
using Json.Pointer;

namespace Json.Schema.DataGeneration.Requirements;

internal class TypeRequirementsGatherer : IRequirementsGatherer
{
	public void AddRequirements(RequirementsContext context, JsonSchemaNode schema, BuildOptions options)
	{
		var typeKeyword = schema.GetKeyword<TypeKeyword>();
		if (typeKeyword == null) return;
		#pragma warning disable CS0618 // Type or member is obsolete
		var typeSource = schema.PathFromResourceRoot.Combine(JsonPointer.Create("type"));
		#pragma warning restore CS0618 // Type or member is obsolete

		if (context.Type == null)
		{
			context.Type = (SchemaValueType)typeKeyword.Value!;
			context.TypeSource = typeSource;
		}
		else
		{
			context.Type &= (SchemaValueType)typeKeyword.Value!;
			context.TypeSource ??= typeSource;
		}
	}
}