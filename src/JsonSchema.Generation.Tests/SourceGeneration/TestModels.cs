using System;
using Json.Schema.Generation.Serialization;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Json.Schema.Generation.Tests.SourceGeneration;

public static class TestModels
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

	[GenerateJsonSchema]
	[If(nameof(Toggle), true, 0)]
	public class SingleCondition
	{
		[Required]
		public bool Toggle { get; set; }

		[Required(ConditionGroup = 0)]
		public string? Required { get; set; }
	}

	[GenerateJsonSchema(PropertyNaming = NamingConvention.CamelCase)]
	[If(nameof(Toggle), true, 0)]
	public class SingleConditionCamelCase
	{
		[Required]
		public bool Toggle { get; set; }

		[Required(ConditionGroup = 0)]
		public string? Required { get; set; }
	}

	[GenerateJsonSchema]
	[If(nameof(Toggle), true, "ifToggle")]
	[If(nameof(OtherToggle), 42, "ifOtherToggle")]
	public class MultipleConditionGroups
	{
		[Required]
		public bool Toggle { get; set; }

		[Required]
		public int OtherToggle { get; set; }

		[Required(ConditionGroup = "ifToggle")]
		public string? RequiredIfToggle { get; set; }

		[Required(ConditionGroup = "ifOtherToggle")]
		public string? RequiredIfOtherToggle { get; set; }
	}

	[GenerateJsonSchema]
	[If(nameof(Count), 1, 0)]
	[If(nameof(Name), "special", 0)]
	public class MultipleTriggersInSameGroup
	{
		[Required]
		public int Count { get; set; }

		[Required]
		public string Name { get; set; } = string.Empty;

		[Required(ConditionGroup = 0)]
		public string? SpecialField { get; set; }
	}

	[GenerateJsonSchema]
	[IfMin(nameof(Age), 18, 0)]
	public class ConditionalWithMinimum
	{
		[Required]
		public int Age { get; set; }

		[Required(ConditionGroup = 0)]
		public string? AdultField { get; set; }
	}

	[GenerateJsonSchema]
	[IfMax(nameof(Score), 100, 0)]
	public class ConditionalWithMaximum
	{
		[Required]
		public int Score { get; set; }

		[Required(ConditionGroup = 0)]
		public string? BonusEligible { get; set; }
	}

	[GenerateJsonSchema]
	[IfEnum(nameof(Day))]
	public class EnumSwitch
	{
		[Required]
		public DayOfWeek Day { get; set; }

		[Required(ConditionGroup = DayOfWeek.Monday)]
		public string? MondayField { get; set; }

		[Required(ConditionGroup = DayOfWeek.Tuesday)]
		public string? TuesdayField { get; set; }
	}

	[GenerateJsonSchema]
	[If(nameof(IsActive), true, 0)]
	public class ConditionalValidation
	{
		[Required]
		public bool IsActive { get; set; }

		[MinLength(5, ConditionGroup = 0)]
		[MaxLength(100, ConditionGroup = 0)]
		public string? Name { get; set; }

		[Minimum(0, ConditionGroup = 0)]
		[Maximum(150, ConditionGroup = 0)]
		public int? Age { get; set; }
	}
}
