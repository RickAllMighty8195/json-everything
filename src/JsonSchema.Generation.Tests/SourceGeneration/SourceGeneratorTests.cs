using NUnit.Framework;
using static Json.Schema.Generation.Tests.AssertionExtensions;

namespace Json.Schema.Generation.Tests.SourceGeneration;

public class SourceGeneratorTests
{
	[Test]
	public void SimplePerson_GeneratesSchema()
	{
		var expectedJson = """
		{
		  "type": "object",
		  "properties": {
		    "Name": { "type": "string" },
		    "Age": { "type": "integer" }
		  },
		  "additionalProperties": false
		}
		""";
		var expected = JsonSchema.FromText(expectedJson);
		var actual = GeneratedJsonSchemas.TestModels_SimplePerson;
		
		AssertEqual(expected, actual);
	}

	[Test]
	public void CamelCasePerson_UsesCamelCase()
	{
		var expectedJson = """
		{
		  "type": "object",
		  "properties": {
		    "firstName": { "type": "string" },
		    "lastName": { "type": "string" },
		    "age": { "type": "integer" }
		  },
		  "additionalProperties": false
		}
		""";
		var expected = JsonSchema.FromText(expectedJson);
		var actual = GeneratedJsonSchemas.TestModels_CamelCasePerson;
		
		AssertEqual(expected, actual);
	}

	[Test]
	public void PersonWithNullable_AllowsNull()
	{
		var expectedJson = """
		{
		  "type": "object",
		  "properties": {
		    "Name": { "type": "string" },
		    "Email": { "type": ["null", "string"] },
		    "Age": { "type": ["null", "integer"] }
		  },
		  "additionalProperties": false
		}
		""";
		var expected = JsonSchema.FromText(expectedJson);
		var actual = GeneratedJsonSchemas.TestModels_PersonWithNullable;
		
		AssertEqual(expected, actual);
	}

	[Test]
	public void PersonWithRequired_MarksRequired()
	{
		var expectedJson = """
		{
		  "type": "object",
		  "properties": {
		    "Name": { "type": "string" },
		    "Age": { "type": "integer" }
		  },
		  "required": ["Name"],
		  "additionalProperties": false
		}
		""";
		var expected = JsonSchema.FromText(expectedJson);
		var actual = GeneratedJsonSchemas.TestModels_PersonWithRequired;
		
		AssertEqual(expected, actual);
	}

	[Test]
	public void PersonWithEnum_HasEnumValues()
	{
		var expectedJson = """
		{
		  "type": "object",
		  "properties": {
		    "Name": { "type": "string" },
		    "Status": {
		      "enum": ["Active", "Inactive", "Pending"]
		    }
		  },
		  "additionalProperties": false
		}
		""";
		var expected = JsonSchema.FromText(expectedJson);
		var actual = GeneratedJsonSchemas.TestModels_PersonWithEnum;
		
		AssertEqual(expected, actual);
	}

	[Test]
	public void PersonWithDescription_HasDescriptions()
	{
		var expectedJson = """
		{
		  "type": "object",
		  "properties": {
		    "Name": {
		      "type": "string",
		      "description": "The person's full name"
		    },
		    "Age": {
		      "type": "integer",
		      "description": "The person's age in years"
		    }
		  },
		  "additionalProperties": false
		}
		""";
		var expected = JsonSchema.FromText(expectedJson);
		var actual = GeneratedJsonSchemas.TestModels_PersonWithDescription;
		
		AssertEqual(expected, actual);
	}

	[Test]
	public void ProductWithCustomAttributes_AppliesCustomEmitters()
	{
		var expectedJson = """
		{
		  "type": "object",
		  "properties": {
		    "Name": { "type": "string" },
		    "Price": {
		      "type": "number",
		      "minimum": 0,
		      "exclusiveMinimum": 0
		    },
		    "DiscountPercentage": {
		      "type": "integer",
		      "minimum": 0,
		      "maximum": 100
		    },
		    "Description": { "type": ["null", "string"] }
		  },
		  "additionalProperties": false
		}
		""";
		var expected = JsonSchema.FromText(expectedJson);
		var actual = GeneratedJsonSchemas.TestModels_ProductWithCustomAttributes;
		
		AssertEqual(expected, actual);
	}

	[Test]
	public void PersonWithAddresses_UsesDefsAndRefs()
	{
		var expectedJson = """
		{
		  "type": "object",
		  "$defs": {
		    "Address": {
		      "type": "object",
		      "properties": {
		        "Street": { "type": "string" },
		        "City": { "type": "string" },
		        "PostalCode": { "type": "string" }
		      },
		      "additionalProperties": false
		    }
		  },
		  "properties": {
		    "Name": { "type": "string" },
		    "HomeAddress": {
		      "description": "Home address",
		      "anyOf": [
		        { "$ref": "#/$defs/Address" },
		        { "type": "null" }
		      ]
		    },
		    "WorkAddress": {
		      "description": "Work address",
		      "anyOf": [
		        { "$ref": "#/$defs/Address" },
		        { "type": "null" }
		      ]
		    }
		  },
		  "additionalProperties": false
		}
		""";
		var expected = JsonSchema.FromText(expectedJson);
		var actual = GeneratedJsonSchemas.TestModels_PersonWithAddresses;
		
		AssertEqual(expected, actual);
	}

	[Test]
	public void SingleCondition_GeneratesIfThen()
	{
		var expectedJson = """
		{
		  "type": "object",
		  "properties": {
		    "Toggle": { "type": "boolean" },
		    "Required": { "type": ["null", "string"] }
		  },
		  "required": ["Toggle"],
		  "if": {
		    "properties": {
		      "Toggle": { "const": true }
		    },
		    "required": ["Toggle"]
		  },
		  "then": {
		    "required": ["Required"]
		  },
		  "additionalProperties": false
		}
		""";
		var expected = JsonSchema.FromText(expectedJson);
		var actual = GeneratedJsonSchemas.TestModels_SingleCondition;
		
		AssertEqual(expected, actual);
	}

	[Test]
	public void SingleConditionCamelCase_HonorsNamingConvention()
	{
		var expectedJson = """
		{
		  "type": "object",
		  "properties": {
		    "toggle": { "type": "boolean" },
		    "required": { "type": ["null", "string"] }
		  },
		  "required": ["toggle"],
		  "if": {
		    "properties": {
		      "toggle": { "const": true }
		    },
		    "required": ["toggle"]
		  },
		  "then": {
		    "required": ["required"]
		  },
		  "additionalProperties": false
		}
		""";
		var expected = JsonSchema.FromText(expectedJson);
		var actual = GeneratedJsonSchemas.TestModels_SingleConditionCamelCase;
		
		AssertEqual(expected, actual);
	}

	[Test]
	public void MultipleConditionGroups_GeneratesMultipleIfThen()
	{
		var expectedJson = """
		{
		  "type": "object",
		  "properties": {
		    "Toggle": { "type": "boolean" },
		    "OtherToggle": { "type": "integer" },
		    "RequiredIfToggle": { "type": ["null", "string"] },
		    "RequiredIfOtherToggle": { "type": ["null", "string"] }
		  },
		  "required": ["Toggle", "OtherToggle"],
		  "allOf": [
		    {
		      "if": {
		        "properties": {
		          "Toggle": { "const": true }
		        },
		        "required": ["Toggle"]
		      },
		      "then": {
		        "required": ["RequiredIfToggle"]
		      }
		    },
		    {
		      "if": {
		        "properties": {
		          "OtherToggle": { "const": 42 }
		        },
		        "required": ["OtherToggle"]
		      },
		      "then": {
		        "required": ["RequiredIfOtherToggle"]
		      }
		    }
		  ],
		  "additionalProperties": false
		}
		""";
		var expected = JsonSchema.FromText(expectedJson);
		var actual = GeneratedJsonSchemas.TestModels_MultipleConditionGroups;
		
		AssertEqual(expected, actual);
	}

	[Test]
	public void MultipleTriggersInSameGroup_CombinesTriggers()
	{
		var expectedJson = """
		{
		  "type": "object",
		  "properties": {
		    "Count": { "type": "integer" },
		    "Name": { "type": "string" },
		    "SpecialField": { "type": ["null", "string"] }
		  },
		  "required": ["Count", "Name"],
		  "if": {
		    "properties": {
		      "Count": { "const": 1 },
		      "Name": { "const": "special" }
		    },
		    "required": ["Count", "Name"]
		  },
		  "then": {
		    "required": ["SpecialField"]
		  },
		  "additionalProperties": false
		}
		""";
		var expected = JsonSchema.FromText(expectedJson);
		var actual = GeneratedJsonSchemas.TestModels_MultipleTriggersInSameGroup;
		
		AssertEqual(expected, actual);
	}

	[Test]
	public void ConditionalWithMinimum_GeneratesMinimumTrigger()
	{
		var expectedJson = """
		{
		  "type": "object",
		  "properties": {
		    "Age": { "type": "integer" },
		    "AdultField": { "type": ["null", "string"] }
		  },
		  "required": ["Age"],
		  "if": {
		    "properties": {
		      "Age": { "minimum": 18 }
		    },
		    "required": ["Age"]
		  },
		  "then": {
		    "required": ["AdultField"]
		  },
		  "additionalProperties": false
		}
		""";
		var expected = JsonSchema.FromText(expectedJson);
		var actual = GeneratedJsonSchemas.TestModels_ConditionalWithMinimum;
		
		AssertEqual(expected, actual);
	}

	[Test]
	public void ConditionalWithMaximum_GeneratesMaximumTrigger()
	{
		var expectedJson = """
		{
		  "type": "object",
		  "properties": {
		    "Score": { "type": "integer" },
		    "BonusEligible": { "type": ["null", "string"] }
		  },
		  "required": ["Score"],
		  "if": {
		    "properties": {
		      "Score": { "maximum": 100 }
		    },
		    "required": ["Score"]
		  },
		  "then": {
		    "required": ["BonusEligible"]
		  },
		  "additionalProperties": false
		}
		""";
		var expected = JsonSchema.FromText(expectedJson);
		var actual = GeneratedJsonSchemas.TestModels_ConditionalWithMaximum;
		
		AssertEqual(expected, actual);
	}

	[Test]
	public void EnumSwitch_GeneratesConditionPerEnumValue()
	{
		var expectedJson = """
		{
		  "type": "object",
		  "properties": {
		    "Day": {
		      "enum": ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"]
		    },
		    "MondayField": { "type": ["null", "string"] },
		    "TuesdayField": { "type": ["null", "string"] }
		  },
		  "required": ["Day"],
		  "allOf": [
		    {
		      "if": {
		        "properties": {
		          "Day": { "const": "Monday" }
		        },
		        "required": ["Day"]
		      },
		      "then": {
		        "required": ["MondayField"]
		      }
		    },
		    {
		      "if": {
		        "properties": {
		          "Day": { "const": "Tuesday" }
		        },
		        "required": ["Day"]
		      },
		      "then": {
		        "required": ["TuesdayField"]
		      }
		    }
		  ],
		  "additionalProperties": false
		}
		""";
		var expected = JsonSchema.FromText(expectedJson);
		var actual = GeneratedJsonSchemas.TestModels_EnumSwitch;
		
		AssertEqual(expected, actual);
	}

	[Test]
	public void ConditionalValidation_EmitsValidationInThenClause()
	{
		var expectedJson = """
		{
		  "type": "object",
		  "properties": {
		    "IsActive": { "type": "boolean" },
		    "Name": { "type": ["null", "string"] },
		    "Age": { "type": ["null", "integer"] }
		  },
		  "required": ["IsActive"],
		  "if": {
		    "properties": {
		      "IsActive": { "const": true }
		    },
		    "required": ["IsActive"]
		  },
		  "then": {
		    "properties": {
		      "Name": {
		        "minLength": 5,
		        "maxLength": 100
		      },
		      "Age": {
		        "minimum": 0,
		        "maximum": 150
		      }
		    }
		  },
		  "additionalProperties": false
		}
		""";
		var expected = JsonSchema.FromText(expectedJson);
		var actual = GeneratedJsonSchemas.TestModels_ConditionalValidation;
		
		AssertEqual(expected, actual);
	}
}

