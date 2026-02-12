using System;

namespace Json.Schema.Generation.Tests.SourceGeneration;

/// <summary>
/// Example custom attribute demonstrating extensibility for source generation.
/// Users can create attributes like this that implement IAttributeHandler
/// and provide their own schema builder logic via a static Apply method.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
public class CustomFormatAttribute : Attribute, IAttributeHandler<CustomFormatAttribute>
{
	public string FormatName { get; }

	public CustomFormatAttribute(string formatName)
	{
		FormatName = formatName;
	}

	/// <summary>
	/// This static method is called by the generated code to apply schema constraints.
	/// The method signature must match: static JsonSchemaBuilder Apply(JsonSchemaBuilder builder, [constructor_args...])
	/// </summary>
	public static JsonSchemaBuilder Apply(JsonSchemaBuilder builder, string formatName)
	{
		return builder.Format(new Format(formatName));
	}

	/// <summary>
	/// Runtime handler method (not used by source generator).
	/// </summary>
	public void AddConstraints(SchemaGenerationContextBase context, Attribute attribute)
	{
		// Runtime implementation not needed for source generation
	}
}

/// <summary>
/// Another example showing a numeric constraint attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PositiveNumberAttribute : Attribute, IAttributeHandler<PositiveNumberAttribute>
{
	/// <summary>
	/// Applies minimum and exclusiveMinimum constraints of 0.
	/// </summary>
	public static JsonSchemaBuilder Apply(JsonSchemaBuilder builder)
	{
		return builder.Minimum(0).ExclusiveMinimum(0);
	}

	/// <summary>
	/// Runtime handler method (not used by source generator).
	/// </summary>
	public void AddConstraints(SchemaGenerationContextBase context, Attribute attribute)
	{
		// Runtime implementation not needed for source generation
	}
}

/// <summary>
/// Example with multiple parameters.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class RangeAttribute : Attribute, IAttributeHandler<RangeAttribute>
{
	public double Min { get; }
	public double Max { get; }

	public RangeAttribute(double min, double max)
	{
		Min = min;
		Max = max;
	}

	public static JsonSchemaBuilder Apply(JsonSchemaBuilder builder, decimal min, decimal max)
	{
		return builder.Minimum(min).Maximum(max);
	}

	/// <summary>
	/// Runtime handler method (not used by source generator).
	/// </summary>
	public void AddConstraints(SchemaGenerationContextBase context, Attribute attribute)
	{
		// Runtime implementation not needed for source generation
	}
}
