#if NET8_0_OR_GREATER

using System.ComponentModel.DataAnnotations;

namespace Json.Schema.Generation.DataAnnotations;

/// <summary>
/// Adds a `format` keyword with `base64`.
/// </summary>
/// <remarks>
/// By default, `format` is an annotation only.  No validation will occur unless configured to do so.
/// 
/// The `base64` format is defined by the OpenAPI 3.1 specification.
/// </remarks>
public class Base64StringAttributeAttributeHandler : FormatAttributeHandler<Base64StringAttribute>
{
	/// <summary>
	/// Creates a new <see cref="Base64StringAttributeAttributeHandler"/>.
	/// </summary>
	public Base64StringAttributeAttributeHandler() : base("base64")
	{
	}

	/// <summary>
	/// Applies constraints for source generation.
	/// </summary>
	/// <param name="builder">The schema builder.</param>
	/// <returns>The builder for chaining.</returns>
	public static JsonSchemaBuilder Apply(JsonSchemaBuilder builder)
	{
		return builder.Format(new Format("base64"));
	}
}

#endif