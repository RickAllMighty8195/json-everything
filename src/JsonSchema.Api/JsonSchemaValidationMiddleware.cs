using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Json.Schema.Api;

internal class JsonSchemaValidationMiddleware
{
	private static readonly JsonSerializerOptions _problemDetailsSerializerOptions = new(JsonSerializerDefaults.Web)
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	private readonly RequestDelegate _next;

	public JsonSchemaValidationMiddleware(RequestDelegate next)
	{
		_next = next;
	}

	public async Task InvokeAsync(HttpContext context)
	{
		var requestBody = await ReadRequestBodyAsync(context);
		var originalResponseBody = context.Response.Body;
		await using var bufferedResponseBody = new MemoryStream();
		context.Response.Body = bufferedResponseBody;

		EvaluationResults? validationResults = null;

		try
		{
			await _next(context);
		}
		catch (BadHttpRequestException exception) when (TryGetValidationResults(exception, out var parsedValidationResults))
		{
			if (context.Response.HasStarted)
				throw;

			validationResults = parsedValidationResults;
		}
		finally
		{
			context.Response.Body = originalResponseBody;
		}

		if (validationResults is null &&
		    TryGetValidationResultsFromRequest(context, requestBody) is { } inferredValidationResults)
		{
			validationResults = inferredValidationResults;
		}

		if (validationResults is not null)
		{
			context.Response.Clear();
			await WriteValidationProblemDetailsAsync(context, validationResults);
			return;
		}

		bufferedResponseBody.Position = 0;
		await bufferedResponseBody.CopyToAsync(originalResponseBody);
	}

	private static async Task<string?> ReadRequestBodyAsync(HttpContext context)
	{
		if (!context.Request.HasJsonContentType()) return null;

		context.Request.EnableBuffering();
		using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
		var body = await reader.ReadToEndAsync();
		context.Request.Body.Position = 0;
		return body;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
	[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
	private static EvaluationResults? TryGetValidationResultsFromRequest(HttpContext context, string? requestBody)
	{
		if (context.Response.StatusCode != StatusCodes.Status400BadRequest) return null;
		if (!string.Equals(context.Response.ContentType, "text/plain; charset=utf-8", StringComparison.OrdinalIgnoreCase) &&
		    !string.Equals(context.Response.ContentType, "text/plain", StringComparison.OrdinalIgnoreCase)) return null;
		if (string.IsNullOrWhiteSpace(requestBody)) return null;

		var requestType = context.GetEndpoint()?.Metadata.GetMetadata<IAcceptsMetadata>()?.RequestType;
		if (requestType is null) return null;

		var options = context.RequestServices
			.GetRequiredService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>()
			.Value
			.SerializerOptions;

		try
		{
			_ = JsonSerializer.Deserialize(requestBody, requestType, options);
			return null;
		}
		catch (JsonException exception) when (TryGetValidationResults(exception, out var validationResults))
		{
			return validationResults;
		}
	}

	private static bool TryGetValidationResults(Exception exception, out EvaluationResults validationResults)
	{
		validationResults = null!;
		for (var current = exception; current is not null; current = current.InnerException)
		{
			if (current is not JsonException jsonException) continue;
			if (!jsonException.Data.Contains("validation")) continue;
			if (jsonException.Data["validation"] is not EvaluationResults { IsValid: false } parsedResults) continue;

			validationResults = parsedResults;
			return true;
		}

		return false;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
	[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
	private static async Task WriteValidationProblemDetailsAsync(HttpContext context, EvaluationResults validationResults)
	{
		var errors = ExtractValidationErrors(validationResults)
			.Where(x => string.IsNullOrEmpty(x.Path) || x.Path.StartsWith('/'))
			.GroupBy(x => x.Path)
			.ToDictionary(x => x.Key, x => x.Select(v => v.Message).ToList());

		if (errors.Count == 0)
			errors[string.Empty] = new List<string> { "A validation error occurred" };

		var problemDetails = new ProblemDetails
		{
			Type = "https://json-everything.net/errors/validation",
			Title = "Validation Error",
			Status = StatusCodes.Status400BadRequest,
			Detail = "One or more validation errors occurred.",
			Extensions =
			{
				["errors"] = errors
			}
		};

		context.Response.StatusCode = StatusCodes.Status400BadRequest;
		context.Response.ContentType = "application/problem+json";
		await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, _problemDetailsSerializerOptions));
	}

	private static List<(string Path, string Message)> ExtractValidationErrors(EvaluationResults validationResults)
	{
		var errors = new List<(string Path, string Message)>();
		ExtractValidationErrorsRecursive(validationResults, errors);
		return errors;
	}

	private static void ExtractValidationErrorsRecursive(EvaluationResults results, List<(string Path, string Message)> errors)
	{
		if (results.IsValid) return;

		if (results.Errors != null)
		{
			foreach (var error in results.Errors)
			{
				errors.Add((results.InstanceLocation.ToString(), error.Value));
			}
		}

		if (results.Details == null) return;
		foreach (var detail in results.Details)
		{
			ExtractValidationErrorsRecursive(detail, errors);
		}
	}
}

internal class JsonSchemaValidationStartupFilter : IStartupFilter
{
	public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
	{
		return app =>
		{
			app.UseMiddleware<JsonSchemaValidationMiddleware>();
			next(app);
		};
	}
}
