using System.ComponentModel.DataAnnotations;

namespace Json.Schema.Generation.DataAnnotations;

/// <summary>
/// Adds a `format` keyword with `email`.
/// </summary>
/// <remarks>
/// By default, `format` is an annotation only.  No validation will occur unless configured to do so.
/// </remarks>
public class EmailAddressAttributeHandler : FormatAttributeHandler<EmailAddressAttribute>
{
	/// <summary>
	/// Creates a new <see cref="EmailAddressAttributeHandler"/>.
	/// </summary>
	public EmailAddressAttributeHandler() : base(Formats.Email)
	{
	}

	/// <summary>
	/// Applies constraints for source generation.
	/// </summary>
	/// <param name="builder">The schema builder.</param>
	/// <returns>The builder for chaining.</returns>
	public static JsonSchemaBuilder Apply(JsonSchemaBuilder builder)
	{
		return builder.Format(Formats.Email);
	}
}