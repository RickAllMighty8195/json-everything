using System.Linq;
using NUnit.Framework;
using static Json.Schema.Generation.Tests.AssertionExtensions;
using static Json.Schema.Generation.Tests.SourceGeneration.TestModels;

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
		  }
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
		  }
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
		  }
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
		  "required": ["Name"]
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
		  }
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
		  }
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
		  }
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
		      }
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
		  }
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
		  }
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
		  }
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
		  ]
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
		  }
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
		  }
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
		  }
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
		  ]
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
		  }
		}
		""";
		var expected = JsonSchema.FromText(expectedJson);
		var actual = GeneratedJsonSchemas.TestModels_ConditionalValidation;
		
		AssertEqual(expected, actual);
	}

	[Test]
	public void PersonWithSortedProperties_SortsPropertiesByName()
	{
		var actual = GeneratedJsonSchemas.TestModels_PersonWithSortedProperties;
		
		// Use Root.Source to get the JSON element
		var propertiesElement = actual.Root.Source.GetProperty("properties");
		
		var propertyNames = propertiesElement.EnumerateObject().Select(p => p.Name).ToList();
		
		// Properties should be sorted alphabetically: Age, City, Email, Name
		Assert.That(propertyNames.Count, Is.EqualTo(4));
		Assert.That(propertyNames[0], Is.EqualTo("Age"));
		Assert.That(propertyNames[1], Is.EqualTo("City"));
		Assert.That(propertyNames[2], Is.EqualTo("Email"));
		Assert.That(propertyNames[3], Is.EqualTo("Name"));
	}

	[Test]
	public void StrictConditionalValidation_ExcludesConditionalPropertiesFromGlobalScope()
	{
		var actual = GeneratedJsonSchemas.TestModels_StrictConditionalValidation;
		
		// In strict mode, properties with conditional constraints should NOT be in global properties
		var propertiesElement = actual.Root.Source.GetProperty("properties");
		var propertyNames = propertiesElement.EnumerateObject().Select(p => p.Name).ToList();
		
		// Only IsActive should be in global properties, not Name or Age
		Assert.That(propertyNames.Count, Is.EqualTo(1));
		Assert.That(propertyNames[0], Is.EqualTo("IsActive"));
	}

	[Test]
	public void StrictConditionalValidation_UsesUnevaluatedProperties()
	{
		var actual = GeneratedJsonSchemas.TestModels_StrictConditionalValidation;
		
		// In strict mode, should use unevaluatedProperties instead of additionalProperties
		Assert.That(actual.Root.Source.TryGetProperty("unevaluatedProperties", out var unevaluatedProps), Is.True);
		Assert.That(unevaluatedProps.GetBoolean(), Is.False);
		
		// Should NOT have additionalProperties
		Assert.That(actual.Root.Source.TryGetProperty("additionalProperties", out _), Is.False);
	}

	[Test]
	public void StrictConditionalValidation_EmitsFullPropertySchemasInThenClause()
	{
		var actual = GeneratedJsonSchemas.TestModels_StrictConditionalValidation;
		
		// Get the if/then structure
		var ifElement = actual.Root.Source.GetProperty("if");
		var thenElement = actual.Root.Source.GetProperty("then");
		
		// Then clause should have properties with full schemas
		var thenProperties = thenElement.GetProperty("properties");
		
		// Name and Age should be in then clause
		Assert.That(thenProperties.TryGetProperty("Name", out var nameSchema), Is.True);
		Assert.That(thenProperties.TryGetProperty("Age", out var ageSchema), Is.True);
		
		// Name should have type definition (full schema)
		Assert.That(nameSchema.TryGetProperty("type", out var nameType), Is.True);
		
		// Age should have type definition (full schema)
		Assert.That(ageSchema.TryGetProperty("type", out var ageType), Is.True);
		
		// Name should have conditional constraints
		Assert.That(nameSchema.TryGetProperty("minLength", out var minLength), Is.True);
		Assert.That(minLength.GetInt32(), Is.EqualTo(5));
		Assert.That(nameSchema.TryGetProperty("maxLength", out var maxLength), Is.True);
		Assert.That(maxLength.GetInt32(), Is.EqualTo(100));
		
		// Age should have conditional constraints
		Assert.That(ageSchema.TryGetProperty("minimum", out var minimum), Is.True);
		Assert.That(minimum.GetInt32(), Is.EqualTo(0));
		Assert.That(ageSchema.TryGetProperty("maximum", out var maximum), Is.True);
		Assert.That(maximum.GetInt32(), Is.EqualTo(150));
	}

	[Test]
	public void ConditionalValidation_NonStrict_IncludesPropertiesGlobally()
	{
		var actual = GeneratedJsonSchemas.TestModels_ConditionalValidation;
		
		// In non-strict mode (default), all properties should be in global properties
		var propertiesElement = actual.Root.Source.GetProperty("properties");
		var propertyNames = propertiesElement.EnumerateObject().Select(p => p.Name).ToList();
		
		// All three properties should be present: IsActive, Name, Age
		Assert.That(propertyNames.Count, Is.EqualTo(3));
		Assert.That(propertyNames, Does.Contain("IsActive"));
		Assert.That(propertyNames, Does.Contain("Name"));
		Assert.That(propertyNames, Does.Contain("Age"));
		
		// In non-strict mode, should not have unevaluatedProperties or additionalProperties
		Assert.That(actual.Root.Source.TryGetProperty("unevaluatedProperties", out _), Is.False);
		Assert.That(actual.Root.Source.TryGetProperty("additionalProperties", out _), Is.False);
	}

	[Test]
	public void SourceGenTarget_MatchesRuntimeGeneration()
	{
		// Compare source-generated schema with runtime generation
		JsonSchema expected = new JsonSchemaBuilder().FromType<SourceGenTarget>();
		JsonSchema actual = GeneratedJsonSchemas.TestModels_SourceGenTarget;
		
		AssertEqual(expected, actual);
	}
}

