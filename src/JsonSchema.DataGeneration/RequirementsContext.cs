using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Json.More;
using Json.Pointer;

namespace Json.Schema.DataGeneration;

internal readonly record struct ErrorReason(string Message, JsonPointer? LeftSchemaLocation = null, JsonPointer? RightSchemaLocation = null);

internal class RequirementsContext
{
	private const SchemaValueType _allTypes =
		SchemaValueType.Array |
		SchemaValueType.Boolean |
		SchemaValueType.Integer |
		SchemaValueType.Null |
		SchemaValueType.Number |
		SchemaValueType.Object |
		SchemaValueType.String;

	public bool IsFalse { get; set; }

	public SchemaValueType? Type { get; set; }
	public SchemaValueType InferredType { get; set; }

	public NumberRangeSet? NumberRanges { get; set; }
	public JsonPointer? NumberRangesSource { get; set; }
	public List<decimal>? Multiples { get; set; }
	public List<decimal>? AntiMultiples { get; set; }

	public NumberRangeSet? StringLengths { get; set; }
	public JsonPointer? StringLengthsSource { get; set; }
	public List<string>? Patterns { get; set; }
	public List<string>? AntiPatterns { get; set; }
	public string? Format { get; set; }
	public JsonPointer? FormatSource { get; set; }

	public List<RequirementsContext>? SequentialItems { get; set; }
	public RequirementsContext? RemainingItems { get; set; }
	public NumberRangeSet? ItemCounts { get; set; }
	// TODO: unevaluatedItems

	public RequirementsContext? Contains { get; set; }
	public NumberRangeSet? ContainsCounts { get; set; }

	public Dictionary<string, RequirementsContext>? Properties { get; set; }
	public RequirementsContext? RemainingProperties { get; set; }
	public RequirementsContext? PropertyNames { get; set; }
	public NumberRangeSet? PropertyCounts { get; set; }
	public List<string>? RequiredProperties { get; set; }
	public List<string>? AvoidProperties { get; set; }
	// TODO: unevaluatedProperties

	public JsonElement? Const { get; set; }
	public bool ConstIsSet { get; set; }
	public JsonPointer? ConstSource { get; set; }
	public List<(bool, JsonElement)>? EnumOptions { get; set; }
	public JsonPointer? EnumSource { get; set; }
	public JsonPointer? TypeSource { get; set; }

	public List<RequirementsContext>? Options { get; set; }

	public bool HasConflict { get; set; }
	public List<ErrorReason>? ErrorReasons { get; set; }

	public RequirementsContext() { }

	public RequirementsContext(RequirementsContext other, bool copyOptions = true)
	{
		Type = other.Type;
		InferredType = other.InferredType;
		IsFalse = other.IsFalse;

		if (other.NumberRanges != null)
			NumberRanges = new NumberRangeSet(other.NumberRanges);
		NumberRangesSource = other.NumberRangesSource;
		if (other.Multiples != null)
			Multiples = [.. other.Multiples];
		if (other.AntiMultiples != null)
			AntiMultiples = [.. other.AntiMultiples];

		if (other.StringLengths != null)
			StringLengths = new NumberRangeSet(other.StringLengths);
		StringLengthsSource = other.StringLengthsSource;
		if (other.Patterns != null)
			Patterns = [.. other.Patterns];
		if (other.AntiPatterns != null)
			AntiPatterns = [.. other.AntiPatterns];
		Format = other.Format;
		FormatSource = other.FormatSource;

		if (other.ItemCounts != null)
			ItemCounts = new NumberRangeSet(other.ItemCounts);
		if (other.SequentialItems != null)
			SequentialItems = other.SequentialItems.Select(x => new RequirementsContext(x)).ToList();
		if (other.RemainingItems != null)
			RemainingItems = new RequirementsContext(other.RemainingItems);
		if (other.Contains != null)
			Contains = new RequirementsContext(other.Contains);
		if (other.ContainsCounts != null)
			ContainsCounts = new NumberRangeSet(other.ContainsCounts);

		if (copyOptions && other.Options != null)
			Options = other.Options.Select(x => new RequirementsContext(x)).ToList();

		Const = other.Const;
		ConstIsSet = other.ConstIsSet;
		ConstSource = other.ConstSource;
		if (other.EnumOptions != null)
			EnumOptions = [.. other.EnumOptions];
		EnumSource = other.EnumSource;
		TypeSource = other.TypeSource;
		HasConflict = other.HasConflict;
		if (other.ErrorReasons != null)
			ErrorReasons = [.. other.ErrorReasons];

		if (other.Properties != null)
			Properties = other.Properties.ToDictionary(x => x.Key, x => new RequirementsContext(x.Value));
		if (other.RemainingProperties != null)
			RemainingProperties = new RequirementsContext(other.RemainingProperties);
		if (other.PropertyNames != null)
			PropertyNames = new RequirementsContext(other.PropertyNames);
		if (other.PropertyCounts != null)
			PropertyCounts = other.PropertyCounts;
		if (other.RequiredProperties != null)
			RequiredProperties = [.. other.RequiredProperties];
		if (other.AvoidProperties != null)
			AvoidProperties = [.. other.AvoidProperties];
	}

	public IEnumerable<RequirementsContext> GetAllVariations()
	{
		RequirementsContext CreateVariation(RequirementsContext option)
		{
			var variation = new RequirementsContext(this, copyOptions: false);
			variation.And(option);
			return variation;
		}

		if (Options == null)
			yield return this;
		else
		{
			using var enumerator = Options.GetEnumerator();
			while (enumerator.MoveNext())
			{
				if (enumerator.Current!.Options != null)
				{
					foreach (var variation in enumerator.Current.GetAllVariations())
					{
						yield return CreateVariation(variation);
					}
				}
				else
					yield return CreateVariation(enumerator.Current);
			}
		}
	}

	// Create a requirements object that doesn't meet this context's requirement
	// Only need to break one requirement for this to work, not all
	public RequirementsContext Break()
	{
		bool BreakBoolean(RequirementsContext context)
		{
			if (IsTrue())
			{
				context.IsFalse = true;
				return true;
			}

			if (IsFalse)
			{
				context.IsFalse = false;
				return true;
			}

			return false;
		}

		bool BreakType(RequirementsContext context)
		{
			if (Type == null) return false;
			context.Type = ~_allTypes ^ ~Type;
			return true;
		}

		bool BreakNumberRange(RequirementsContext context)
		{
			if (NumberRanges == null || !NumberRanges.Ranges.Any()) return false;
			context.NumberRanges = NumberRanges?.GetComplement();
			return true;
		}

		bool BreakMultiples(RequirementsContext context)
		{
			if (Multiples == null && AntiMultiples == null) return false;
			context.Multiples = AntiMultiples;
			context.AntiMultiples = Multiples;
			return true;
		}

		bool BreakStringLength(RequirementsContext context)
		{
			if (StringLengths == null || !StringLengths.Ranges.Any()) return false;
			context.StringLengths = StringLengths?.GetComplement();
			return true;
		}

		bool BreakPatterns(RequirementsContext context)
		{
			if ((Patterns == null || Patterns.Count == 0) && (AntiPatterns == null || AntiPatterns.Count == 0))
				return false;

			context.Patterns = AntiPatterns != null ? [.. AntiPatterns] : null;
			context.AntiPatterns = Patterns != null ? [.. Patterns] : null;

			return true;
		}

		bool BreakFormat(RequirementsContext context)
		{
			if (Format == null) return false;

			context.Format = null;
			context.AntiPatterns ??= [];
			if (Format == Formats.Date.Key)
				context.AntiPatterns.Add("^\\d{4}-\\d{2}-\\d{2}$");

			return true;
		}

		bool BreakItems(RequirementsContext context)
		{
			if (RemainingItems == null) return false;
			context.RemainingItems = RemainingItems.Break();
			return true;
		}

		bool BreakItemCount(RequirementsContext context)
		{
			if (ItemCounts == null) return false;
			context.ItemCounts = ItemCounts?.GetComplement();
			return true;
		}

		bool BreakProperties(RequirementsContext context)
		{
			if (RemainingProperties != null)
			{
				context.RemainingProperties = RemainingProperties.Break();
				return true;
			}

			if (Properties != null)
			{
				var forbiddenProperties = Properties
					.Where(x => x.Value.IsFalse)
					.Select(x => x.Key)
					.ToArray();
				if (forbiddenProperties.Length > 0)
				{
					context.Properties = Properties.ToDictionary(
						x => x.Key,
						x => x.Value.IsFalse ? new RequirementsContext() : x.Value.Break());
					context.RequiredProperties ??= [];
					context.RequiredProperties.AddRange(forbiddenProperties);
					return true;
				}

				context.Properties = Properties.ToDictionary(x => x.Key, x => x.Value.Break());
				context.RequiredProperties ??= [];
				context.RequiredProperties.AddRange(context.Properties.Where(x => !x.Value.IsFalse).Select(x => x.Key));
				return true;
			}

			return false;
		}

		bool BreakRequired(RequirementsContext context)
		{
			if (RequiredProperties == null && AvoidProperties == null) return false;
			context.RequiredProperties = AvoidProperties;
			context.AvoidProperties = RequiredProperties;
			return true;
		}

		bool BreakPropertyNames(RequirementsContext context)
		{
			if (PropertyNames == null) return false;
			context.PropertyNames = PropertyNames.Break();
			return true;
		}

		bool BreakPropertyCounts(RequirementsContext context)
		{
			if (PropertyCounts == null) return false;
			context.PropertyCounts = PropertyCounts?.GetComplement();
			return true;
		}

		bool BreakContains(RequirementsContext context)
		{
			if (Contains == null) return false;
			context.Contains = Contains.Break();
			return true;
		}

		bool BreakContainsCount(RequirementsContext context)
		{
			if (ContainsCounts == null) return false;
			context.ContainsCounts = ContainsCounts?.GetComplement();
			return true;
		}

		bool BreakConst(RequirementsContext context)
		{
			if (!ConstIsSet) return false;
			context.Const = Guid.NewGuid().ToString().AsJsonElement();
			context.ConstIsSet = true;
			return true;
		}

		var allBreakers = new[]
		{
			BreakBoolean,
			BreakType,
			BreakNumberRange,
			BreakMultiples,
			BreakStringLength,
			BreakPatterns,
			BreakFormat,
			BreakItems,
			BreakItemCount,
			BreakProperties,
			BreakRequired,
			BreakPropertyNames,
			BreakPropertyCounts,
			BreakContains,
			BreakContainsCount,
			BreakConst
		};
		var breakers = JsonSchemaExtensions.Randomizer.Shuffle(allBreakers);

		var broken = new RequirementsContext(this);
		using var enumerator = breakers.GetEnumerator();
		while (enumerator.MoveNext() && !enumerator.Current!(broken)) { }

		return broken;
	}

	public void And(RequirementsContext other)
	{
		void AddConflict(string reason, JsonPointer? leftSchemaLocation = null, JsonPointer? rightSchemaLocation = null)
		{
			HasConflict = true;
			ErrorReasons ??= [];
			if (ErrorReasons.Count < 5)
				ErrorReasons.Add(new ErrorReason(reason, leftSchemaLocation, rightSchemaLocation));
		}

		static string DescribeTypes(SchemaValueType? types)
		{
			if (types == null) return "any";
			return types.Value.ToString();
		}

		static string DescribeConst(JsonElement? value)
		{
			return value?.GetRawText() ?? "null";
		}

		static string DescribeRanges(NumberRangeSet rangeSet)
		{
			if (!rangeSet.Ranges.Any()) return "(empty)";
			return string.Join(", ", rangeSet.Ranges.Select(x => x.ToString()));
		}

		static string DescribeEnumOptions(List<(bool, JsonElement)> options)
		{
			var values = options.Select(x => x.Item2.GetRawText()).Distinct().Take(5).ToArray();
			var suffix = options.Count > 5 ? ", ..." : string.Empty;
			return $"[{string.Join(", ", values)}{suffix}]";
		}

		static List<RequirementsContext> MergeSequentialItems(RequirementsContext leftContext, RequirementsContext rightContext)
		{
			var leftSequentialItems = leftContext.SequentialItems!;
			var rightSequentialItems = rightContext.SequentialItems!;
			var maxLength = Math.Max(leftSequentialItems.Count, rightSequentialItems.Count);
			var merged = new List<RequirementsContext>(maxLength);

			RequirementsContext GetRequirementForIndex(RequirementsContext context, int index)
			{
				if (context.SequentialItems != null && index < context.SequentialItems.Count)
					return new RequirementsContext(context.SequentialItems[index]);

				if (context.RemainingItems != null)
					return new RequirementsContext(context.RemainingItems);

				return new RequirementsContext();
			}

			for (var i = 0; i < maxLength; i++)
			{
				var left = GetRequirementForIndex(leftContext, i);
				left.And(GetRequirementForIndex(rightContext, i));
				merged.Add(left);
			}

			return merged;
		}

		IsFalse |= other.IsFalse;
		if (other.ErrorReasons != null)
		{
			ErrorReasons ??= [];
			foreach (var reason in other.ErrorReasons)
			{
				if (ErrorReasons.Count >= 5) break;
				if (!ErrorReasons.Contains(reason))
					ErrorReasons.Add(reason);
			}
		}

		if (Type == null)
		{
			Type = other.Type;
			TypeSource = other.TypeSource;
		}
		else if (other.Type != null)
		{
			var thisType = Type;
			var thisTypeSource = TypeSource;
			Type &= other.Type;
			if (Type == 0)
				AddConflict($"Conflicting type constraints have no overlap: {DescribeTypes(thisType)} vs {DescribeTypes(other.Type)}.", thisTypeSource, other.TypeSource);
		}

		InferredType |= other.InferredType;

		if (NumberRanges == null || !NumberRanges.Ranges.Any())
		{
			NumberRanges = other.NumberRanges;
			NumberRangesSource = other.NumberRangesSource;
		}
		else if (other.NumberRanges != null)
		{
			var thisRangesDescription = DescribeRanges(NumberRanges);
			var otherRangesDescription = DescribeRanges(other.NumberRanges);
			NumberRanges *= other.NumberRanges;
			if (!NumberRanges.Ranges.Any())
				AddConflict($"Conflicting numeric ranges have no overlap: {thisRangesDescription} vs {otherRangesDescription}.", NumberRangesSource, other.NumberRangesSource);
		}

		if (Multiples == null)
			Multiples = other.Multiples;
		else if (other.Multiples != null)
			Multiples.AddRange(other.Multiples);

		if (AntiMultiples == null)
			AntiMultiples = other.AntiMultiples;
		else if (other.AntiMultiples != null)
			AntiMultiples.AddRange(other.AntiMultiples);

		if (StringLengths == null || !StringLengths.Ranges.Any())
		{
			StringLengths = other.StringLengths;
			StringLengthsSource = other.StringLengthsSource;
		}
		else if (other.StringLengths != null)
		{
			StringLengths *= other.StringLengths;
			if (!StringLengths.Ranges.Any())
				AddConflict("Conflicting string length constraints have no overlap.", StringLengthsSource, other.StringLengthsSource);
		}

		if (Format == null)
		{
			Format = other.Format;
			FormatSource = other.FormatSource;
		}
		else if (other.Format != null)
		{
			if (Format != other.Format)
				AddConflict($"Conflicting format constraints: '{Format}' vs '{other.Format}'.", FormatSource, other.FormatSource);
		}

		if (!ConstIsSet)
		{
			Const = other.Const;
			ConstIsSet = other.ConstIsSet;
			ConstSource = other.ConstSource;
		}
		else if (other.ConstIsSet)
		{
			if (!(Const?.IsEquivalentTo(other.Const!.Value) ?? false))
				AddConflict($"Conflicting const values: {DescribeConst(Const)} vs {DescribeConst(other.Const)}.", ConstSource, other.ConstSource);
		}

		if (EnumOptions == null)
		{
			EnumOptions = other.EnumOptions != null ? [.. other.EnumOptions] : null;
			EnumSource = other.EnumSource;
		}
		else if (other.EnumOptions != null)
		{
			var thisEnumDescription = DescribeEnumOptions(EnumOptions);
			var otherEnumDescription = DescribeEnumOptions(other.EnumOptions);
			EnumOptions = EnumOptions.Where(x => other.EnumOptions.Any(y => x.Item2.IsEquivalentTo(y.Item2))).ToList();
			if (!EnumOptions.Any())
				AddConflict($"Conflicting enum options have no overlap: {thisEnumDescription} vs {otherEnumDescription}.", EnumSource, other.EnumSource);
		}

		if (Patterns == null)
			Patterns = other.Patterns != null ? [.. other.Patterns] : null;
		else if (other.Patterns != null)
			Patterns.AddRange(other.Patterns);

		if (AntiPatterns == null)
			AntiPatterns = other.AntiPatterns != null ? [.. other.AntiPatterns] : null;
		else if (other.AntiPatterns != null)
			AntiPatterns.AddRange(other.AntiPatterns);

		if (ItemCounts == null || !ItemCounts.Ranges.Any())
			ItemCounts = other.ItemCounts;
		else if (other.ItemCounts != null)
			ItemCounts *= other.ItemCounts;

		if (SequentialItems == null)
			SequentialItems = other.SequentialItems?.Select(x => new RequirementsContext(x)).ToList();
		else if (other.SequentialItems != null)
			SequentialItems = MergeSequentialItems(this, other);

		if (RemainingItems == null)
			RemainingItems = other.RemainingItems;
		else if (other.RemainingItems != null)
			RemainingItems.And(other.RemainingItems);

		if (PropertyCounts == null || !PropertyCounts.Ranges.Any())
			PropertyCounts = other.PropertyCounts;
		else if (other.PropertyCounts != null)
			PropertyCounts *= other.PropertyCounts;

		if (Properties == null)
			Properties = other.Properties;
		else if (other.Properties != null)
		{
			var allKeys = Properties.Keys.Union(other.Properties.Keys);
			foreach (var key in allKeys)
			{
				Properties.TryGetValue(key, out var thisProperty);
				other.Properties.TryGetValue(key, out var otherProperty);

				if (thisProperty == null)
					Properties[key] = otherProperty!;
				else if (otherProperty != null)
					thisProperty.And(otherProperty);
			}
		}

		if (RemainingProperties == null)
			RemainingProperties = other.RemainingProperties;
		else if (other.RemainingProperties != null)
			RemainingProperties.And(other.RemainingProperties);

		if (PropertyNames == null)
			PropertyNames = other.PropertyNames;
		else if (other.PropertyNames != null)
			PropertyNames.And(other.PropertyNames);

		if (RequiredProperties == null)
			RequiredProperties = other.RequiredProperties;
		else if (other.RequiredProperties != null)
			RequiredProperties.AddRange(other.RequiredProperties);

		if (AvoidProperties == null)
			AvoidProperties = other.AvoidProperties;
		else if (other.AvoidProperties != null)
			AvoidProperties.AddRange(other.AvoidProperties);

		if (Contains == null)
			Contains = other.Contains;
		else if (other.Contains != null)
			// is this right?
			Contains.And(other.Contains);

		if (ContainsCounts == null || !ContainsCounts.Ranges.Any())
			ContainsCounts = other.ContainsCounts;
		else if (other.ContainsCounts != null)
			ContainsCounts *= other.ContainsCounts;

		if (Options == null)
			Options = other.Options?.Select(x => new RequirementsContext(x)).ToList();
		else if (other.Options != null)
		{
			var combined = new List<RequirementsContext>(Options.Count * other.Options.Count);
			foreach (var thisOption in Options)
			{
				foreach (var otherOption in other.Options)
				{
					var option = new RequirementsContext(thisOption);
					option.And(new RequirementsContext(otherOption));
					combined.Add(option);
				}
			}

			Options = combined;
		}
	}

	private bool IsTrue()
	{
		return !IsFalse &&
		       Type == null &&
		       NumberRanges == null &&
		       Multiples == null &&
		       AntiMultiples == null &&
		       StringLengths == null &&
		       Patterns == null &&
		       AntiPatterns == null &&
		       Format == null &&
		       SequentialItems == null &&
		       RemainingItems == null &&
		       ItemCounts == null &&
		       Contains == null &&
		       ContainsCounts == null &&
		       Properties == null &&
		       RemainingProperties == null &&
		       PropertyNames == null &&
		       PropertyCounts == null &&
		       RequiredProperties == null &&
		       AvoidProperties == null &&
		       Const == null &&
		       !ConstIsSet &&
		       EnumOptions == null &&
		       Options == null;
	}
}