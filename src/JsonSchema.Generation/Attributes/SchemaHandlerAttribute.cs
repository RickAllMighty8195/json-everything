using System;

namespace Json.Schema.Generation;

/// <summary>
/// Registers a source generation schema handler for a target type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class SchemaHandlerAttribute : Attribute
{
	/// <summary>
	/// The target type handled by the attributed handler.
	/// </summary>
	public Type TargetType { get; }

	/// <summary>
	/// Creates a new <see cref="SchemaHandlerAttribute"/> instance.
	/// </summary>
	/// <param name="targetType">The target type handled by the schema handler.</param>
	public SchemaHandlerAttribute(Type targetType)
	{
		TargetType = targetType;
	}
}
