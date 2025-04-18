﻿using System;
using System.Collections;
using Json.More;
using Json.Schema.Generation.Intents;

namespace Json.Schema.Generation;

/// <summary>
/// Creates or amends a condition group by defining an expected maximum value in a property.
/// </summary>
/// <remarks>
/// The specific keywords which are added depend on the type of the targeted property.
/// </remarks>
[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = true)]
public class IfMaxAttribute : ConditionalAttribute, INestableAttribute, IConditionalAttribute
{
	/// <summary>
	/// The property name.
	/// </summary>
	public string PropertyName { get; set; }
	
	/// <summary>
	/// The expected maximum value.
	/// </summary>
	public double Value { get; set; }
	
	/// <summary>
	/// Gets or sets whether the value should be exclusive.
	/// </summary>
	public bool IsExclusive { get; set; }

	/// <summary>
	/// The index of the parameter to which the attribute should apply. Default is -1 to indicate the root.
	/// </summary>
	public int GenericParameter { get; set; } = -1;

	internal Type? PropertyType { get; set; }

	/// <summary>
	/// Creates a new <see cref="IfAttribute"/> instance.
	/// </summary>
	/// <param name="propertyName">The name of the property.</param>
	/// <param name="value">The expected maximum value for the property.</param>
	/// <param name="group">The condition group.</param>
	/// <remarks>
	/// The <paramref name="value"/> parameter is provided as a `double` but stored as a `decimal`
	/// because `decimal` is not a valid attribute parameter type.
	/// As such, to prevent overflows, the value is clamped to the `decimal` range prior to being converted
	/// when applied as the `maximum` or `exclusiveMaximum` keywords.
	/// </remarks>
	public IfMaxAttribute(string propertyName, double value, object? group)
	{
		PropertyName = propertyName;
		Value = value;
		ConditionGroup = group;
	}

	internal ISchemaKeywordIntent? GetIntent()
	{
		if (PropertyType == null) return null;

		if (PropertyType.IsNumber() || PropertyType.IsNullableNumber())
		{
			if (IsExclusive) return new ExclusiveMaximumIntent(Value.ClampToDecimal());
			return new MaximumIntent(Value.ClampToDecimal());
		}

		// ReSharper disable once CompareOfFloatsByEqualityOperator
		if (Value < 0 && Value != Math.Floor(Value)) return null;

		if (PropertyType == typeof(string))
		{
			return new MaxLengthIntent((uint)Value);
		}

		if (typeof(IEnumerable).IsAssignableFrom(PropertyType) &&
		    !typeof(IDictionary).IsAssignableFrom(PropertyType))
			return new MaxItemsIntent((uint)Value);

		return new MaxPropertiesIntent((uint)Value);
	}
}