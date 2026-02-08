using NUnit.Framework;
using static Json.Schema.Generation.Tests.AssertionExtensions;

namespace Json.Schema.Generation.Tests.SourceGeneration;

public class SourceGeneratorTests
{
	[Test]
	public void SimplePerson_GeneratesSchema()
	{
		var expected = new JsonSchemaBuilder()
			.Type(SchemaValueType.Object)
			.Properties(
				("Name", new JsonSchemaBuilder().Type(SchemaValueType.String)),
				("Age", new JsonSchemaBuilder().Type(SchemaValueType.Integer)))
			.AdditionalProperties(false)
			.Build();

		var actual = GeneratedJsonSchemas.TestModels_SimplePerson;
		
		AssertEqual(expected, actual);
	}

	[Test]
	public void CamelCasePerson_UsesCamelCase()
	{
		var expected = new JsonSchemaBuilder()
			.Type(SchemaValueType.Object)
			.Properties(
				("firstName", new JsonSchemaBuilder().Type(SchemaValueType.String)),
				("lastName", new JsonSchemaBuilder().Type(SchemaValueType.String)),
				("age", new JsonSchemaBuilder().Type(SchemaValueType.Integer)))
			.AdditionalProperties(false)
			.Build();

		var actual = GeneratedJsonSchemas.TestModels_CamelCasePerson;
		
		AssertEqual(expected, actual);
	}

	[Test]
	public void PersonWithNullable_AllowsNull()
	{
		var expected = new JsonSchemaBuilder()
			.Type(SchemaValueType.Object)
			.Properties(
				("Name", new JsonSchemaBuilder().Type(SchemaValueType.String)),
				("Email", new JsonSchemaBuilder().Type(SchemaValueType.String, SchemaValueType.Null)),
				("Age", new JsonSchemaBuilder().Type(SchemaValueType.Integer, SchemaValueType.Null)))
			.AdditionalProperties(false)
			.Build();

		var actual = GeneratedJsonSchemas.TestModels_PersonWithNullable;
		
		AssertEqual(expected, actual);
	}

	[Test]
	public void PersonWithRequired_MarksRequired()
	{
		var expected = new JsonSchemaBuilder()
			.Type(SchemaValueType.Object)
			.Properties(
				("Name", new JsonSchemaBuilder().Type(SchemaValueType.String)),
				("Age", new JsonSchemaBuilder().Type(SchemaValueType.Integer)))
			.Required("Name")
			.AdditionalProperties(false)
			.Build();

		var actual = GeneratedJsonSchemas.TestModels_PersonWithRequired;
		
		AssertEqual(expected, actual);
	}

	[Test]
	public void PersonWithEnum_HasEnumValues()
	{
		var expected = new JsonSchemaBuilder()
			.Type(SchemaValueType.Object)
			.Properties(
				("Name", new JsonSchemaBuilder().Type(SchemaValueType.String)),
				("Status", new JsonSchemaBuilder()
					.Type(SchemaValueType.String)
					.Enum("Active", "Inactive", "Pending")))
			.AdditionalProperties(false)
			.Build();

		var actual = GeneratedJsonSchemas.TestModels_PersonWithEnum;
		
		AssertEqual(expected, actual);
	}

	[Test]
	public void PersonWithDescription_HasDescriptions()
	{
		var expected = new JsonSchemaBuilder()
			.Type(SchemaValueType.Object)
			.Properties(
				("Name", new JsonSchemaBuilder()
					.Type(SchemaValueType.String)
					.Description("The person's full name")),
				("Age", new JsonSchemaBuilder()
					.Type(SchemaValueType.Integer)
					.Description("The person's age in years")))
			.AdditionalProperties(false)
			.Build();

		var actual = GeneratedJsonSchemas.TestModels_PersonWithDescription;
		
		AssertEqual(expected, actual);
	}

	[Test]
	public void ProductWithCustomAttributes_AppliesCustomEmitters()
	{
		var expected = new JsonSchemaBuilder()
			.Type(SchemaValueType.Object)
			.Properties(
				("Name", new JsonSchemaBuilder().Type(SchemaValueType.String)),
				("Price", new JsonSchemaBuilder()
					.Type(SchemaValueType.Number)
					.Minimum(0)
					.ExclusiveMinimum(0)),
				("DiscountPercentage", new JsonSchemaBuilder()
					.Type(SchemaValueType.Integer)
					.Minimum(0)
					.Maximum(100)),
				("Description", new JsonSchemaBuilder()
					.Type(SchemaValueType.String, SchemaValueType.Null)))
			.AdditionalProperties(false)
			.Build();

		var actual = GeneratedJsonSchemas.TestModels_ProductWithCustomAttributes;
		
		AssertEqual(expected, actual);
	}

	[Test]
	public void PersonWithAddresses_UsesDefsAndRefs()
	{
		var expected = new JsonSchemaBuilder()
			.Type(SchemaValueType.Object)
			.Defs(
				("Address", new JsonSchemaBuilder()
					.Type(SchemaValueType.Object)
					.Properties(
						("Street", new JsonSchemaBuilder().Type(SchemaValueType.String)),
						("City", new JsonSchemaBuilder().Type(SchemaValueType.String)),
						("PostalCode", new JsonSchemaBuilder().Type(SchemaValueType.String)))
					.AdditionalProperties(false)))
			.Properties(
				("Name", new JsonSchemaBuilder().Type(SchemaValueType.String)),
				("HomeAddress", new JsonSchemaBuilder()
					.Description("Home address")
					.AnyOf(
						new JsonSchemaBuilder().Ref("#/$defs/Address"),
						new JsonSchemaBuilder().Type(SchemaValueType.Null))),
				("WorkAddress", new JsonSchemaBuilder()
					.Description("Work address")
					.AnyOf(
						new JsonSchemaBuilder().Ref("#/$defs/Address"),
						new JsonSchemaBuilder().Type(SchemaValueType.Null))))
			.AdditionalProperties(false)
			.Build();

		var actual = GeneratedJsonSchemas.TestModels_PersonWithAddresses;
		
		AssertEqual(expected, actual);
	}
}

