using System.Linq;

namespace Json.Schema.DataGeneration.Generators;

internal class EnumGenerator : IPrioritizedDataGenerator
{
	public static EnumGenerator Instance { get; } = new();

	private EnumGenerator() { }

	public bool Applies(RequirementsContext context)
	{
		return context.EnumOptions?.Any(x => x.Item1) == true;
	}

	public GenerationResult Generate(RequirementsContext context)
	{
		var value = JsonSchemaExtensions.Randomizer.ListItem(context.EnumOptions!.Where(x => x.Item1).ToList());
		return GenerationResult.Success(value.Item2);
	}
}