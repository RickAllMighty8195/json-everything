using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Pointer;

namespace Json.Schema.DataGeneration;

/// <summary>
/// Holds the result of an instance generation operation.
/// </summary>
public class GenerationResult
{
	/// <summary>
	/// Gets the resulting JSON data, if successful.
	/// </summary>
	public JsonNode? Result { get; }
	/// <summary>
	/// Gets the error message from the generation, if unsuccessful.
	/// </summary>
	public string? ErrorMessage { get; }
	/// <summary>
	/// Gets the result objects from nested data generations.
	/// </summary>
	public IEnumerable<GenerationResult>? InnerResults { get; }
	/// <summary>
	/// Gets the relative instance location at which this failure occurred, if known.
	/// </summary>
	public JsonPointer? Location { get; }
	/// <summary>
	/// Gets the related schema locations for this failure, if known.
	/// </summary>
	public IReadOnlyList<JsonPointer>? SchemaLocations { get; }

	/// <summary>
	/// Gets whether the data generation was successful.
	/// </summary>
	public bool IsSuccess => ErrorMessage == null && InnerResults == null;

	private GenerationResult(JsonNode? result, string? errorMessage, IEnumerable<GenerationResult>? inner, JsonPointer? location, IReadOnlyList<JsonPointer>? schemaLocations)
	{
		Result = result ?? null;
		ErrorMessage = errorMessage;
		InnerResults = inner;
		Location = location;
		SchemaLocations = schemaLocations;
	}

	internal static GenerationResult Success(JsonElement? result)
	{
		return new GenerationResult(JsonNode.Parse(result!.Value.GetRawText()), null, null, null, null);
	}

	internal static GenerationResult Success(JsonNode? result)
	{
		return new GenerationResult(result, null, null, null, null);
	}

	internal static GenerationResult Fail(string errorMessage, JsonPointer? location = null, IReadOnlyList<JsonPointer>? schemaLocations = null)
	{
		return new GenerationResult(null, errorMessage, null, location, schemaLocations);
	}

	internal static GenerationResult Fail(IEnumerable<GenerationResult> inner, JsonPointer? location = null, IReadOnlyList<JsonPointer>? schemaLocations = null)
	{
		return new GenerationResult(null, null, inner, location, schemaLocations);
	}
}