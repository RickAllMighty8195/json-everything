using System;

namespace Json.Schema.Generation.Serialization;

/// <summary>
/// Apply to a type to generate a schema for validation during deserialization
/// by <see cref="GenerativeValidatingJsonConverter"/>.
/// </summary>
/// <remarks>
/// When applied, source generation will create a static property containing a pre-built
/// <see cref="JsonSchema"/> for the decorated type at compile time.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
public class GenerateJsonSchemaAttribute : Attribute
{
	/// <summary>
	/// Gets or sets the naming convention to use for property names in the generated schema.
	/// </summary>
	/// <remarks>
	/// Defaults to <see cref="NamingConvention.AsDeclared"/> if not specified.
	/// </remarks>
	public NamingConvention PropertyNaming { get; set; } = NamingConvention.CamelCase;

	/// <summary>
	/// Gets or sets the order in which properties will be listed in the schema.
	/// </summary>
	/// <remarks>
	/// Defaults to <see cref="PropertyOrder.AsDeclared"/> if not specified.
	/// </remarks>
	public PropertyOrder PropertyOrder { get; set; } = PropertyOrder.AsDeclared;

	/// <summary>
	/// Gets or sets whether properties affected by conditionals are defined globally
	/// or only within their respective then subschemas.
	/// </summary>
	/// <remarks>
	/// When true, restricts conditional property definitions to `then` subschemas and adds
	/// a top-level `unevaluatedProperties: false`. When false (default), defines them globally.
	/// </remarks>
	public bool StrictConditionals { get; set; }
}
