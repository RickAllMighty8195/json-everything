using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
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

	[GenerateJsonSchema(PropertyOrder = PropertyOrder.ByName)]
	public class PersonWithSortedProperties
	{
		public string Name { get; set; } = string.Empty;
		public int Age { get; set; }
		public string Email { get; set; } = string.Empty;
		public string City { get; set; } = string.Empty;
	}

	[GenerateJsonSchema(StrictConditionals = true)]
	[If(nameof(IsActive), true, 0)]
	public class StrictConditionalValidation
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

	public enum SourceGenTargetEnumeration
	{
		[JsonIgnore]
		IgnoreThis,

		[JsonIgnore(Condition = JsonIgnoreCondition.Never)]
		DontIgnoreThis,

		[JsonExclude]
		IgnoreThisWithJsonExclude,

		[JsonIgnore]
		[JsonExclude]
		IgnoreAndExcludeThis,

		[JsonIgnore(Condition = JsonIgnoreCondition.Never)]
		[JsonExclude]
		ExcludeTrumpsIgnore,

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		[JsonExclude]
		ExcludeTrumpsIgnoreWhenWritingDefault,

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		DontIgnoreThisWhenWritingDefault,

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		DontIgnoreThisWhenWritingNull,
	}

	[GenerateJsonSchema]
	public class SourceGenTarget
	{
		[JsonInclude]
#pragma warning disable CS0169
		private int _value;

		private int _notIncluded;
#pragma warning restore CS0169

		public SourceGenTargetEnumeration EnumProp { get; set; }

		[Required]
		[Minimum(5)]
		[ExclusiveMinimum(4)]
		[Maximum(10)]
		[ExclusiveMaximum(11)]
		[MultipleOf(1.5)]
		public int Integer { get; set; }

		[MaxLength(10)]
		[Pattern("^[a-z0-9_]$")]
		public string String { get; set; } = string.Empty;

		[Required]
		public string RequiredString { get; set; } = string.Empty;

		[JsonPropertyName("rename-this-required-string")]
		[Required]
		public string RenameThisRequiredString { get; set; } = string.Empty;

		[MinItems(5)]
		[MaxItems(10)]
		public List<bool> ListOfBool { get; set; }

		[MinLength(5, GenericParameter = 0)]
		[UniqueItems(true)]
		[Obsolete]
		public List<string> ListOfString { get; set; }

		[Maximum(100)]
		public int Duplicated1 { get; set; }

		[Maximum(100)]
		public int Duplicated2 { get; set; }

		public SourceGenTarget Target { get; set; }

		[JsonIgnore]
		public int IgnoreThis { get; set; }

		[JsonIgnore(Condition = JsonIgnoreCondition.Never)]
		public string DontIgnoreThis { get; set; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public string DontIgnoreThisWhenWritingDefault { get; set; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string DontIgnoreThisWhenWritingNull { get; set; }

		[JsonExclude]
		public string IgnoreThisWithJsonExclude { get; set; }

		[JsonIgnore]
		[JsonExclude]
		public double IgnoreAndExcludeThis { get; set; }

		[JsonIgnore(Condition = JsonIgnoreCondition.Never)]
		[JsonExclude]
		public double ExcludeTrumpsIgnore { get; set; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		[JsonExclude]
		public double ExcludeTrumpsIgnoreWhenWritingDefault { get; set; }

		[JsonPropertyName("rename-this")]
		public string RenameThis { get; set; }

		public float StrictNumber { get; set; }
		public float OtherStrictNumber { get; set; }

		[ReadOnly]
		public float ReadOnlyNumber { get; set; }

		[WriteOnly]
		public float WriteOnlyNumber { get; set; }

		[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
		public float StringyNumber { get; set; }

		[JsonNumberHandling(JsonNumberHandling.AllowNamedFloatingPointLiterals)]
		public float NotANumber { get; set; }

		[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals)]
		public float StringyNotANumber { get; set; }

		[Title("title")]
		[Description("description")]
		public string Metadata { get; set; }
	}
}
