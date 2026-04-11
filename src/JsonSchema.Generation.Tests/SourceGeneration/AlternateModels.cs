using Json.Schema.Generation.Serialization;

namespace Json.Schema.Generation.Tests.SourceGeneration.AlternateNamespace;

public class AlternateModels
{
	[GenerateJsonSchema]
	public class CrossNamespacePerson
	{
		public string Name { get; set; } = string.Empty;
		public int Age { get; set; }
	}
}
