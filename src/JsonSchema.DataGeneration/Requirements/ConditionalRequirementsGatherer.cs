using System.Linq;
using Json.Schema.Keywords;

namespace Json.Schema.DataGeneration.Requirements;

internal class ConditionalRequirementsGatherer : IRequirementsGatherer
{
	public void AddRequirements(RequirementsContext context, JsonSchemaNode schema, BuildOptions options)
	{
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
			var ifRequirement = ifKeyword.Subschemas[0].GetRequirements(options);

			RequirementsContext? ifthen = null;
			RequirementsContext? thenOnly = null;
			if (thenKeyword != null)
			{
				thenOnly = thenKeyword.Subschemas[0].GetRequirements(options);
				ifthen = new RequirementsContext(ifRequirement);
				ifthen.And(new RequirementsContext(thenOnly));
			}

			RequirementsContext? ifelse = null;
			RequirementsContext? elseOnly = null;
			if (elseKeyword != null)
			{
				elseOnly = elseKeyword.Subschemas[0].GetRequirements(options);
				ifelse = new RequirementsContext(ifRequirement.Break());
				ifelse.And(new RequirementsContext(elseOnly));
			}

			if (ifthen == null && ifelse == null) return;
			if (ifthen == null)
				AddOptions(ifRequirement, elseOnly!);
			else if (ifelse == null)
				AddOptions(ifthen, thenOnly!);
			else
				AddOptions(ifthen, ifelse);
		}
	}
}