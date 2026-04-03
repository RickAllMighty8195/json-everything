using System.Text.Json.Nodes;
using NUnit.Framework;

using static Json.Schema.DataGeneration.Tests.TestRunner;

namespace Json.Schema.DataGeneration.Tests;

public class ConstGenerationTests
{
	[Test]
	public void ConstSchemaGeneratesItsValue()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		var schema = new JsonSchemaBuilder(buildOptions)
			.Const("this is totally a random string");

		Run(schema, buildOptions);
	}

	[Test]
	public void NotConstSchemaGeneratesAnythingButItsValue()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		var schema = new JsonSchemaBuilder(buildOptions)
			.Not(new JsonSchemaBuilder()
				.Const("anything but this value"));

		Run(schema, buildOptions);
	}

	[Test]
	public void AllOfWithSameConstValueGenerates()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		var schema = new JsonSchemaBuilder(buildOptions)
			.AllOf(
				new JsonSchemaBuilder().Const("same value"),
				new JsonSchemaBuilder().Const("same value")
			);

		Run(schema, buildOptions);
	}

	[Test]
	public void AllOfWithDifferentConstValuesFails()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		var schema = new JsonSchemaBuilder(buildOptions)
			.AllOf(
				new JsonSchemaBuilder().Const("value 1"),
				new JsonSchemaBuilder().Const("value 2")
			);

		RunFailure(schema, buildOptions);
	}

	[Test]
	public void ConstNull()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		var schema = new JsonSchemaBuilder(buildOptions)
			.Const((JsonNode?)null);

		Run(schema, buildOptions);
	}

	[Test]
	public void ConstBoolean()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		var schema = new JsonSchemaBuilder(buildOptions)
			.Const(true);

		Run(schema, buildOptions);
	}

	[Test]
	public void ConstNumber()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		var schema = new JsonSchemaBuilder(buildOptions)
			.Const(42);

		Run(schema, buildOptions);
	}
}