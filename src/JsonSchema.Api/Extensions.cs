using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using Json.Schema.Generation;
using Json.Schema.Generation.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Json.Schema.Api;

/// <summary>
/// Provides extension methods for configuring JSON Schema validation in ASP.NET Core MVC applications.
/// </summary>
/// <remarks>The extension methods in this class enable integration of JSON Schema-based validation into the MVC
/// request pipeline. These methods are intended to be used during application startup to add model validation based on
/// JSON Schema definitions. All methods are static and designed for use with dependency injection and MVC configuration
/// patterns.</remarks>
public static class Extensions
{
	/// <summary>
	/// Enables JSON Schema-based validation for MVC controllers by adding the necessary filters and model binders to the
	/// application's MVC pipeline.
	/// </summary>
	/// <remarks>This method registers a filter that validates incoming JSON request bodies against their associated
	/// JSON Schemas and configures the JSON serializer to support schema-based validation. Use this method to enforce
	/// schema compliance for models in your MVC application.</remarks>
	/// <param name="builder">The MVC builder to configure. Cannot be null.</param>
	/// <param name="configure">An optional delegate to configure the generative JSON schema validation converter. If null, default settings are
	/// used.</param>
	/// <returns>The same <see cref="IMvcBuilder"/> instance so that additional configuration calls can be chained.</returns>
	/// <remarks>
	/// By default, source generation is used to create schemas at compile time from types decorated with <c>[GenerateJsonSchema]</c>.
	/// When using source generation, settings like property naming, property order, and strict conditionals must be configured on the
	/// <c>[GenerateJsonSchema]</c> attribute itself. The <see cref="GenerativeValidatingJsonConverter.GeneratorConfiguration"/> settings
	/// only affect runtime schema generation, which occurs when source generation is disabled or when using <c>[JsonSchema]</c> attributes.
	/// To disable source generation, add <c>&lt;DisableJsonSchemaSourceGeneration&gt;true&lt;/DisableJsonSchemaSourceGeneration&gt;</c>
	/// to your project file.
	/// </remarks>
	[RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize(Utf8JsonWriter, Object, Type, JsonSerializerOptions)")]
	[RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize(Utf8JsonWriter, Object, Type, JsonSerializerOptions)")]
	public static IMvcBuilder AddJsonSchemaValidation(this IMvcBuilder builder, Action<GenerativeValidatingJsonConverter>? configure = null)
	{
		builder.Services.Configure<MvcOptions>(options =>
		{
			if (!options.Filters.Any(x => x is JsonSchemaValidationFilter))
				options.Filters.Add<JsonSchemaValidationFilter>();
			if (!options.ModelBinderProviders.Any(x => x is ValidatingJsonModelBinderProvider))
				options.ModelBinderProviders.Insert(0, new ValidatingJsonModelBinderProvider());
		});

		builder.AddJsonOptions(opt =>
		{
			opt.JsonSerializerOptions.AddJsonSchemaValidation(configure);
		});

		return builder;
	}

	[RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize(Utf8JsonWriter, Object, Type, JsonSerializerOptions)")]
	[RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize(Utf8JsonWriter, Object, Type, JsonSerializerOptions)")]
	private static JsonSerializerOptions AddJsonSchemaValidation(this JsonSerializerOptions options, Action<GenerativeValidatingJsonConverter>? configure = null)
	{
		var converter = options.Converters.FirstOrDefault(x => x is GenerativeValidatingJsonConverter) as GenerativeValidatingJsonConverter ??
		                new GenerativeValidatingJsonConverter();

		if (configure is null)
		{
			converter.GeneratorConfiguration.PropertyNameResolver = PropertyNameResolvers.CamelCase;
			converter.EvaluationOptions.OutputFormat = OutputFormat.Hierarchical;
			converter.EvaluationOptions.RequireFormatValidation = true;
		}
		else
		{
			configure(converter);
		}

		if (!options.Converters.Contains(converter))
			options.Converters.Add(converter);

		return options;
	}
}