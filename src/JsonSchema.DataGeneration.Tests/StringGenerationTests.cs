using System;
using NUnit.Framework;
using TestHelpers;
using static Json.Schema.DataGeneration.Tests.TestRunner;

namespace Json.Schema.DataGeneration.Tests;

public class StringGenerationTests
{
	[Test]
	public void SimpleString()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String);

		Run(schema, buildOptions);
	}

	[Test]
	public void MinLength()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.MinLength(30);

		Run(schema, buildOptions);
	}

	[Test]
	public void MaxLength()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.MaxLength(20);

		Run(schema, buildOptions);
	}

	[Test]
	public void SpecifiedRange()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.MinLength(10)
			.MaxLength(20);

		Run(schema, buildOptions);
	}

	[Test]
	public void ContainsDog()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Pattern("dog");

		Run(schema, buildOptions);
	}

	[Test]
	public void ContainsDogWithSizeConstraints()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Pattern("dog")
			.MinLength(10)
			.MaxLength(20);

		Run(schema, buildOptions);
	}

	[Test]
	public void DoesNotContainDog()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Not(new JsonSchemaBuilder().Pattern("dog"));

		Run(schema, buildOptions);
	}

	[Test]
	public void DoesNotContainDogWithSizeConstraints()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Not(new JsonSchemaBuilder().Pattern("dog"))
			.MinLength(10)
			.MaxLength(20);

		Run(schema, buildOptions);
	}

	[Test]
	public void ContainsCatAndDoesNotContainDogWithSizeConstraints()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Not(new JsonSchemaBuilder().Pattern("dog"))
			.Pattern("cat")
			.MinLength(10)
			.MaxLength(20);

		Run(schema, buildOptions);
	}

	[Test]
	public void ContainsEitherCatOrDog()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.AnyOf(
				new JsonSchemaBuilder().Pattern("dog"),
				new JsonSchemaBuilder().Pattern("cat")
			)
			.MinLength(10)
			.MaxLength(20);

		Run(schema, buildOptions);
	}

	[Test]
	public void ContainsExclusivelyEitherCatOrDog()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.OneOf(
				new JsonSchemaBuilder().Pattern("dog"),
				new JsonSchemaBuilder().Pattern("cat")
			)
			.MinLength(10)
			.MaxLength(20);

		Run(schema, buildOptions);
	}

	[Test]
	public void FormatDate()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Format(Formats.Date);

		Run(schema, buildOptions);
	}

	[Test]
	public void FormatDateTime()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Format(Formats.DateTime);

		Run(schema, buildOptions);
	}

	[Test]
	public void FormatDuration()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Format(Formats.Duration);

		Run(schema, buildOptions);
	}

	[Test]
	public void FormatEmail()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Format(Formats.Email);

		Run(schema, buildOptions);
	}

	[Test]
	public void FormatHostname()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Format(Formats.Hostname);

		Run(schema, buildOptions);
	}

	[Test]
	public void FormatIdnEmail()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Format(Formats.IdnEmail);

		Run(schema, buildOptions);
	}

	[Test]
	public void FormatIdnHostname()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Format(Formats.IdnHostname);

		Run(schema, buildOptions);
	}

	[Test]
	public void FormatIpv4()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Format(Formats.Ipv4);

		Run(schema, buildOptions);
	}

	[Test]
	public void FormatIpv6()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Format(Formats.Ipv6);

		Run(schema, buildOptions);
	}

	[Test]
	public void FormatIri()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Format(Formats.Iri);

		Run(schema, buildOptions);
	}

	[Test]
	public void FormatIriReference()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder()
			.Type(SchemaValueType.String)
			.Format(Formats.IriReference);

		Run(schema, buildOptions);
	}

	[Test]
	public void FormatJsonPointer()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Format(Formats.JsonPointer);

		Run(schema, buildOptions);
	}

	[Test]
	public void FormatRelativeJsonPointer()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Format(Formats.RelativeJsonPointer);

		Run(schema, buildOptions);
	}

	[Test]
	public void FormatTime()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Format(Formats.Time);

		Run(schema, buildOptions);
	}

	[Test]
	public void FormatUri()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Format(Formats.Uri);

		Run(schema, buildOptions);
	}

	[Test]
	public void FormatUriReference()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Format(Formats.UriReference);

		Run(schema, buildOptions);
	}

	[Test]
	public void FormatUuid()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Format(Formats.Uuid);

		Run(schema, buildOptions);
	}

	[Test]
	public void MultipleFormats()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.AllOf(
				new JsonSchemaBuilder()
					.Type(SchemaValueType.String)
					.Format(Formats.DateTime),
				new JsonSchemaBuilder()
					.Type(SchemaValueType.String)
					.Format(Formats.Uuid)
			);

		var result = schema.GenerateData(buildOptions);

		TestConsole.WriteLine(result.ErrorMessage);
		Assert.That(result.IsSuccess, Is.False);
	}

	[Test]
	public void PatternDigitsOnly()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Pattern(@"^\d+$");

		Run(schema, buildOptions);
	}

	[Test]
	public void PatternWordCharacters()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Pattern(@"^\w{5,10}$");

		Run(schema, buildOptions);
	}

	[Test]
	public void PatternCharacterClass()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Pattern(@"^[a-z]{3}$");

		Run(schema, buildOptions);
	}

	[Test]
	public void PatternAlternation()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Pattern(@"^(foo|bar|baz)$");

		Run(schema, buildOptions);
	}

	[Test]
	public void PatternExplicitQuantifier()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Pattern(@"^\d{4}-\d{2}-\d{2}$");

		Run(schema, buildOptions);
	}

	[Test]
	public void PatternWithOptionalGroup()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Pattern(@"^https?://\S+$");

		Run(schema, buildOptions);
	}

	[Test]
	public void PatternNegatedCharacterClass()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Pattern(@"^[^0-9]+$");

		Run(schema, buildOptions);
	}

	// ---- Pattern + length ----

	[Test]
	public void PatternWithMinLength()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Pattern("cat")
			.MinLength(5);

		Run(schema, buildOptions);
	}

	[Test]
	public void PatternWithMinAndMaxLength()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Pattern("cat")
			.MinLength(5)
			.MaxLength(15);

		Run(schema, buildOptions);
	}

	// ---- allOf: multiple required patterns (AND) ----

	[Test]
	public void AllOfTwoCompatiblePatterns()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.AllOf(
				new JsonSchemaBuilder().Pattern("cat"),
				new JsonSchemaBuilder().Pattern("hat")
			)
			.MinLength(6)
			.MaxLength(30);

		Run(schema, buildOptions);
	}

	[Test]
	public void AllOfPatternAndLength()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.AllOf(
				new JsonSchemaBuilder().Type(SchemaValueType.String).Pattern("foo").MinLength(5),
				new JsonSchemaBuilder().Type(SchemaValueType.String).MaxLength(20)
			);

		Run(schema, buildOptions);
	}

	[Test]
	public void AllOfWithSamePatternIsNotAConflict()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.AllOf(
				new JsonSchemaBuilder().Pattern("dog"),
				new JsonSchemaBuilder().Pattern("dog")
			);

		Run(schema, buildOptions);
	}

	// ---- not: anti-pattern (NOT) ----

	[Test]
	public void NotPatternDigits()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Not(new JsonSchemaBuilder().Pattern(@"\d"))
			.MinLength(5)
			.MaxLength(20);

		Run(schema, buildOptions);
	}

	[Test]
	public void NotPatternWithExplicitLiteral()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Not(new JsonSchemaBuilder().Pattern("forbidden"))
			.MinLength(5)
			.MaxLength(30);

		Run(schema, buildOptions);
	}

	// ---- Positive AND negative combined ----

	[Test]
	public void PatternRequiredAndForbiddenCombined()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.Pattern("cat")
			.Not(new JsonSchemaBuilder().Pattern("dog"))
			.MinLength(5)
			.MaxLength(30);

		Run(schema, buildOptions);
	}

	[Test]
	public void TwoRequiredAndOneForbiddenPattern()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.AllOf(
				new JsonSchemaBuilder().Pattern("cat"),
				new JsonSchemaBuilder().Pattern("hat")
			)
			.Not(new JsonSchemaBuilder().Pattern("dog"))
			.MinLength(6)
			.MaxLength(30);

		Run(schema, buildOptions);
	}

	// ---- anyOf / oneOf ----

	[Test]
	public void AnyOfPatternNoLengthConstraint()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.AnyOf(
				new JsonSchemaBuilder().Pattern("cat"),
				new JsonSchemaBuilder().Pattern("dog")
			);

		Run(schema, buildOptions);
	}

	[Test]
	public void OneOfPatternNoLengthConstraint()
	{
		var buildOptions = new BuildOptions { SchemaRegistry = new() };
		JsonSchema schema = new JsonSchemaBuilder(buildOptions)
			.Type(SchemaValueType.String)
			.OneOf(
				new JsonSchemaBuilder().Pattern("cat"),
				new JsonSchemaBuilder().Pattern("dog")
			);

		Run(schema, buildOptions);
	}
}