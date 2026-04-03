using Json.Pointer;
using Json.Schema.Keywords;

namespace Json.Schema.DataGeneration.Requirements;

internal class ConditionalRequirementsGatherer : IRequirementsGatherer
{
	public void AddRequirements(RequirementsContext context, JsonSchemaNode schema, BuildOptions options)
	{
		RequirementsContext GetBranchRequirements(KeywordData keyword, JsonPointer branchPath)
		{
			var branchBuildContext = BuildContext.From(keyword) with
			{
				LocalSchema = keyword.RawValue,
				RelativePath = JsonPointer.Empty,
				#pragma warning disable CS0618 // Type or member is obsolete
				PathFromResourceRoot = branchPath
				#pragma warning restore CS0618 // Type or member is obsolete
			};

			var branchNode = JsonSchema.BuildNode(branchBuildContext);
			return branchNode.GetRequirements(options);
		}

		void AddOptions(params RequirementsContext[] optionContexts)
		{
			context.Options ??= [];
			context.Options.AddRange(optionContexts);
		}

		var ifKeyword = schema.GetKeyword<IfKeyword>();
		var thenKeyword = schema.GetKeyword<ThenKeyword>();
		var elseKeyword = schema.GetKeyword<ElseKeyword>();

		if (ifKeyword != null)
		{
			#pragma warning disable CS0618 // Type or member is obsolete
			var sourceRoot = schema.PathFromResourceRoot;
			#pragma warning restore CS0618 // Type or member is obsolete

			var ifRequirement = GetBranchRequirements(ifKeyword, sourceRoot.Combine(JsonPointer.Create("if")));

			RequirementsContext? ifthen = null;
			if (thenKeyword != null)
			{
				var thenOnly = GetBranchRequirements(thenKeyword, sourceRoot.Combine(JsonPointer.Create("then")));
				ifthen = new RequirementsContext(ifRequirement);
				ifthen.And(new RequirementsContext(thenOnly));
			}

			RequirementsContext? ifelse = null;
			if (elseKeyword != null)
			{
				var elseOnly = GetBranchRequirements(elseKeyword, sourceRoot.Combine(JsonPointer.Create("else")));
				ifelse = new RequirementsContext(ifRequirement.Break());
				ifelse.And(new RequirementsContext(elseOnly));
			}

			if (ifthen == null && ifelse == null) return;
			if (ifthen == null)
				AddOptions(ifRequirement, ifelse!);
			else if (ifelse == null)
				AddOptions(ifthen, ifRequirement.Break());
			else
				AddOptions(ifthen, ifelse);
		}
	}
}