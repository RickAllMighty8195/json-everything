using Json.Schema.Generation.Serialization;

namespace Json.Schema.Generation.Tests.SourceGeneration;

/// <summary>
/// Test models for source generator testing.
/// </summary>
internal static class TestModels
{
	[GenerateJsonSchema]
	public class SimplePerson
	{
		public string Name { get; set; } = string.Empty;
		public int Age { get; set; }
	}

	[GenerateJsonSchema(PropertyNaming = NamingConvention.CamelCase)]
	public class CamelCasePerson
	{
		public string FirstName { get; set; } = string.Empty;
		public string LastName { get; set; } = string.Empty;
		public int Age { get; set; }
	}

	[GenerateJsonSchema]
	public class PersonWithNullable
	{
		public string Name { get; set; } = string.Empty;
		public string? Email { get; set; }
		public int? Age { get; set; }
	}

	[GenerateJsonSchema]
	public class PersonWithRequired
	{
		public required string Name { get; set; }
		public int Age { get; set; }
	}

	// Skip validation attributes test for now - will add later

	// Skip validation attributes test for now - will add later

	public enum Status
	{
		Active,
		Inactive,
		Pending
	}

	[GenerateJsonSchema]
	public class PersonWithEnum
	{
		public string Name { get; set; } = string.Empty;
		public Status Status { get; set; }
	}

	[GenerateJsonSchema]
	public class PersonWithDescription
	{
		/// <summary>
		/// The person's full name
		/// </summary>
		public string Name { get; set; } = string.Empty;

		/// <summary>
		/// The person's age in years
		/// </summary>
		public int Age { get; set; }
	}

	[GenerateJsonSchema]
	public class ProductWithCustomAttributes
	{
		public string Name { get; set; } = string.Empty;

		[PositiveNumber]
		public decimal Price { get; set; }

		[Range(0, 100)]
		public int DiscountPercentage { get; set; }

		public string? Description { get; set; }
	}

	// Test model for $defs/$ref generation
	public class Address
	{
		public string Street { get; set; } = string.Empty;
		public string City { get; set; } = string.Empty;
		public string PostalCode { get; set; } = string.Empty;
	}

	[GenerateJsonSchema]
	public class PersonWithAddresses
	{
		public string Name { get; set; } = string.Empty;
		[Description("Home address")]
		public Address? HomeAddress { get; set; }
		[Description("Work address")]
		public Address? WorkAddress { get; set; }
	}
}
