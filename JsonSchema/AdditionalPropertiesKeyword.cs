﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.More;
using Json.Pointer;

namespace Json.Schema;

/// <summary>
/// Handles `additionalProperties`.
/// </summary>
[SchemaPriority(10)]
[SchemaKeyword(Name)]
[SchemaSpecVersion(SpecVersion.Draft6)]
[SchemaSpecVersion(SpecVersion.Draft7)]
[SchemaSpecVersion(SpecVersion.Draft201909)]
[SchemaSpecVersion(SpecVersion.Draft202012)]
[SchemaSpecVersion(SpecVersion.DraftNext)]
[Vocabulary(Vocabularies.Applicator201909Id)]
[Vocabulary(Vocabularies.Applicator202012Id)]
[Vocabulary(Vocabularies.ApplicatorNextId)]
[DependsOnAnnotationsFrom(typeof(PropertiesKeyword))]
[DependsOnAnnotationsFrom(typeof(PatternPropertiesKeyword))]
[JsonConverter(typeof(AdditionalPropertiesKeywordJsonConverter))]
public class AdditionalPropertiesKeyword : IJsonSchemaKeyword, ISchemaContainer, IEquatable<AdditionalPropertiesKeyword>, IConstrainer
{
	/// <summary>
	/// The JSON name of the keyword.
	/// </summary>
	public const string Name = "additionalProperties";

	/// <summary>
	/// The schema by which to evaluate additional properties.
	/// </summary>
	public JsonSchema Schema { get; }

	/// <summary>
	/// Creates a new <see cref="AdditionalPropertiesKeyword"/>.
	/// </summary>
	/// <param name="value">The keyword's schema.</param>
	public AdditionalPropertiesKeyword(JsonSchema value)
	{
		Schema = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Performs evaluation for the keyword.
	/// </summary>
	/// <param name="context">Contextual details for the evaluation process.</param>
	public void Evaluate(EvaluationContext context)
	{
		context.EnterKeyword(Name);
		var schemaValueType = context.LocalInstance.GetSchemaValueType();
		if (schemaValueType != SchemaValueType.Object)
		{
			context.WrongValueKind(schemaValueType);
			return;
		}

		context.Options.LogIndentLevel++;
		var overallResult = true;
		List<string> evaluatedProperties;
		var obj = (JsonObject)context.LocalInstance!;
		if (!obj.VerifyJsonObject()) return;

		if (context.Options.EvaluatingAs is SpecVersion.Draft6 or SpecVersion.Draft7)
		{
			evaluatedProperties = new List<string>();
			if (context.LocalSchema.TryGetKeyword<PropertiesKeyword>(PropertiesKeyword.Name, out var propertiesKeyword))
				evaluatedProperties.AddRange(propertiesKeyword!.Properties.Keys);
			if (context.LocalSchema.TryGetKeyword<PatternPropertiesKeyword>(PatternPropertiesKeyword.Name, out var patternPropertiesKeyword))
				evaluatedProperties.AddRange(obj
					.Select(x => x.Key)
					.Where(x => patternPropertiesKeyword!.Patterns.Any(p => p.Key.IsMatch(x))));
		}
		else
		{
			if (!context.LocalResult.TryGetAnnotation(PropertiesKeyword.Name, out var annotation))
			{
				context.Log(() => $"No annotation from {PropertiesKeyword.Name}.");
				evaluatedProperties = new List<string>();
			}
			else
			{
				// ReSharper disable once AccessToModifiedClosure
				context.Log(() => $"Annotation from {PropertiesKeyword.Name}: {annotation.AsJsonString()}");
				evaluatedProperties = annotation!.AsArray().Select(x => x!.GetValue<string>()).ToList();
			}
			if (!context.LocalResult.TryGetAnnotation(PatternPropertiesKeyword.Name, out annotation))
				context.Log(() => $"No annotation from {PatternPropertiesKeyword.Name}.");
			else
			{
				context.Log(() => $"Annotation from {PatternPropertiesKeyword.Name}: {annotation.AsJsonString()}");
				evaluatedProperties.AddRange(annotation!.AsArray().Select(x => x!.GetValue<string>()));
			}
		}
		var additionalProperties = obj.Where(p => !evaluatedProperties.Contains(p.Key)).ToArray();
		evaluatedProperties.Clear();
		foreach (var property in additionalProperties)
		{
			if (!obj.TryGetPropertyValue(property.Key, out var item))
			{
				context.Log(() => $"Property '{property.Key}' does not exist. Skipping.");
				continue;
			}

			context.Log(() => $"Evaluating property '{property.Key}'.");
			context.Push(context.InstanceLocation.Combine(property.Key), item ?? JsonNull.SignalNode,
				context.EvaluationPath.Combine(Name), Schema);
			context.Evaluate();
			var localResult = context.LocalResult.IsValid;
			overallResult &= localResult;
			context.Log(() => $"Property '{property.Key}' {localResult.GetValidityString()}.");
			context.Pop();
			if (!overallResult && context.ApplyOptimizations) break;
			if (localResult)
				evaluatedProperties.Add(property.Key);
		}
		context.Options.LogIndentLevel--;

		context.LocalResult.SetAnnotation(Name, JsonSerializer.SerializeToNode(evaluatedProperties));

		if (!overallResult)
			context.LocalResult.Fail();
		context.ExitKeyword(Name, context.LocalResult.IsValid);
	}

	public KeywordConstraint GetConstraint(SchemaConstraint schemaConstraint, IReadOnlyList<KeywordConstraint> localConstraints, ConstraintBuilderContext context)
	{
		var propertiesConstraint = localConstraints.FirstOrDefault(x => x.Keyword == PropertiesKeyword.Name);
		var patternPropertiesConstraint = localConstraints.FirstOrDefault(x => x.Keyword == PatternPropertiesKeyword.Name);
		var keywordConstraints = propertiesConstraint != null
			? patternPropertiesConstraint != null
				? new[] { propertiesConstraint, patternPropertiesConstraint }
				: new[] { propertiesConstraint }
			: patternPropertiesConstraint != null
				? new[] { patternPropertiesConstraint }
				: Array.Empty<KeywordConstraint>();

		var subschemaConstraint = Schema.GetConstraint(JsonPointer.Create(Name), JsonPointer.Empty, context);
		subschemaConstraint.InstanceLocationGenerator = evaluation =>
		{
			// TODO: json node value type checks
			// TODO: simplify this process
			var properties = evaluation.LocalInstance.AsObject().Select(x => x.Key);
			var propertiesEvaluation = evaluation.KeywordEvaluations.FirstOrDefault(x => x.Constraint.Keyword == PropertiesKeyword.Name);
			if (propertiesEvaluation != null)
				properties = properties.Except(propertiesEvaluation.SubschemaEvaluations.Select(x => x.RelativeInstanceLocation.Segments[0].Value));
			var patternPropertiesEvaluation = evaluation.KeywordEvaluations.FirstOrDefault(x => x.Constraint.Keyword == PatternPropertiesKeyword.Name);
			if (patternPropertiesEvaluation != null)
				properties = properties.Except(patternPropertiesEvaluation.SubschemaEvaluations.Select(x => x.RelativeInstanceLocation.Segments[0].Value));

			return properties.Select(x => JsonPointer.Create(x));
		};

		return new KeywordConstraint(Name, Evaluator)
		{
			KeywordDependencies = keywordConstraints,
			SubschemaDependencies = new[] { subschemaConstraint }
		};
	}

	private static void Evaluator(KeywordEvaluation evaluation)
	{
		var schemaValueType = evaluation.LocalInstance.GetSchemaValueType();
		if (schemaValueType is not SchemaValueType.Object) return;

		if (!evaluation.SubschemaEvaluations.All(x => x.Results.IsValid))
			evaluation.Results.Fail();
	}

	/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
	/// <param name="other">An object to compare with this object.</param>
	/// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
	public bool Equals(AdditionalPropertiesKeyword? other)
	{
		if (ReferenceEquals(null, other)) return false;
		if (ReferenceEquals(this, other)) return true;
		return Equals(Schema, other.Schema);
	}

	/// <summary>Determines whether the specified object is equal to the current object.</summary>
	/// <param name="obj">The object to compare with the current object.</param>
	/// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
	public override bool Equals(object obj)
	{
		return Equals(obj as AdditionalPropertiesKeyword);
	}

	/// <summary>Serves as the default hash function.</summary>
	/// <returns>A hash code for the current object.</returns>
	public override int GetHashCode()
	{
		return Schema.GetHashCode();
	}
}

internal class AdditionalPropertiesKeywordJsonConverter : JsonConverter<AdditionalPropertiesKeyword>
{
	public override AdditionalPropertiesKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var schema = JsonSerializer.Deserialize<JsonSchema>(ref reader, options)!;

		return new AdditionalPropertiesKeyword(schema);
	}
	public override void Write(Utf8JsonWriter writer, AdditionalPropertiesKeyword value, JsonSerializerOptions options)
	{
		writer.WritePropertyName(AdditionalPropertiesKeyword.Name);
		JsonSerializer.Serialize(writer, value.Schema, options);
	}
}