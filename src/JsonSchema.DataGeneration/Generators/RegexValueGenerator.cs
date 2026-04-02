using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Bogus;

namespace Json.Schema.DataGeneration.Generators;

internal static class RegexValueGenerator
{
	private const int _maxPatternGenerationAttempts = 128;
	private const string _fallbackAlphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 -_./";
	private static readonly Faker _faker = new();

	public static GenerationResult Generate(IEnumerable<string>? requiredPatterns, IEnumerable<string>? antiPatterns, string? format, uint minimum, uint maximum)
	{
		var required = requiredPatterns?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() ?? [];
		var forbidden = antiPatterns?.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList() ?? [];

		if (required.Count == 0 && forbidden.Count == 0)
			return GenerationResult.Fail("No regex constraints were provided.");

		var requiredRegexes = new List<Regex>(required.Count);
		foreach (var pattern in required)
		{
			try
			{
				requiredRegexes.Add(new Regex(pattern));
			}
			catch (ArgumentException ex)
			{
				return GenerationResult.Fail($"Cannot parse required regex pattern '{pattern}': {ex.Message}");
			}
		}

		var antiRegexes = new List<Regex>(forbidden.Count);
		foreach (var pattern in forbidden)
		{
			try
			{
				antiRegexes.Add(new Regex(pattern));
			}
			catch (ArgumentException ex)
			{
				return GenerationResult.Fail($"Cannot parse forbidden regex pattern '{pattern}': {ex.Message}");
			}
		}

		bool IsValidCandidate(string candidate)
		{
			if (candidate.Length < minimum || candidate.Length > maximum)
				return false;

			if (requiredRegexes.Any(regex => !regex.IsMatch(candidate)))
				return false;

			if (antiRegexes.Any(regex => regex.IsMatch(candidate)))
				return false;

			if (!string.IsNullOrEmpty(format) && !MatchesKnownFormat(candidate, format))
				return false;

			return true;
		}

		for (var i = 0; i < _maxPatternGenerationAttempts; i++)
		{
			var candidate = CreatePatternCandidate(required, format, minimum, maximum);
			if (IsValidCandidate(candidate))
				return GenerationResult.Success(candidate);
		}

		return GenerationResult.Fail("Could not generate data matching combined regex constraints.");
	}

	private static string CreatePatternCandidate(List<string> requiredPatterns, string? format, uint minimum, uint maximum)
	{
		if (!string.IsNullOrWhiteSpace(format) && TryGenerateKnownFormatMatch(format, minimum, maximum, out var knownFormatMatch))
			return knownFormatMatch;

		if (requiredPatterns.Count == 0)
			return GenerateNoise((int)JsonSchemaExtensions.Randomizer.UInt(minimum, maximum));

		var targetLength = (int)JsonSchemaExtensions.Randomizer.UInt(minimum, maximum);

		// Generate a fragment from every required pattern so that all are satisfied.
		// Anchored patterns govern the whole string, so only one can win — use the last one.
		string? anchoredCore = null;
		var fragments = new List<string>(requiredPatterns.Count);
		foreach (var pattern in requiredPatterns)
		{
			var alternatives = SplitTopLevelAlternatives(pattern);
			var chosen = JsonSchemaExtensions.Randomizer.ArrayElement(alternatives.ToArray());
			var fragment = GenerateFromRegexFragment(chosen, (int)maximum);
			if (IsAnchored(pattern))
				anchoredCore = fragment;
			else
				fragments.Add(fragment);
		}

		if (anchoredCore != null)
			return anchoredCore;

		// Shuffle fragments so the sub-strings appear in random order.
		var shuffled = JsonSchemaExtensions.Randomizer.Shuffle(fragments).ToList();
		var core = string.Concat(shuffled);

		if (targetLength <= 0 || core.Length >= targetLength)
			return core;

		var extraLength = targetLength - core.Length;
		// Scatter fragments with noise between them for broader coverage.
		var prefixLength = JsonSchemaExtensions.Randomizer.Int(0, extraLength);
		var suffixLength = extraLength - prefixLength;
		return $"{GenerateNoise(prefixLength)}{core}{GenerateNoise(suffixLength)}";
	}

	private static string GenerateNoise(int length)
	{
		if (length <= 0) return string.Empty;

		var chars = new char[length];
		for (var i = 0; i < length; i++)
		{
			chars[i] = _fallbackAlphabet[JsonSchemaExtensions.Randomizer.Int(0, _fallbackAlphabet.Length - 1)];
		}

		return new string(chars);
	}

	private static bool TryGenerateKnownFormatMatch(string? format, uint minimum, uint maximum, out string match)
	{
		List<string> candidates =
		[
			_faker.Person.Email,
			_faker.Internet.UrlWithPath(),
			Guid.NewGuid().ToString("D"),
			_faker.Internet.DomainName(),
			_faker.Internet.Ip(),
			_faker.Internet.Ipv6()
		];

		if (!string.IsNullOrWhiteSpace(format))
		{
			var lowered = format!.ToLowerInvariant();
			candidates = lowered switch
			{
				"email" or "idn-email" => [_faker.Person.Email],
				"uri" or "iri" or "uri-reference" or "iri-reference" => [_faker.Internet.UrlWithPath()],
				"uuid" => [Guid.NewGuid().ToString("D")],
				"hostname" or "idn-hostname" => [_faker.Internet.DomainName()],
				"ipv4" => [_faker.Internet.Ip()],
				"ipv6" => [_faker.Internet.Ipv6()],
				_ => candidates
			};
		}

		foreach (var candidate in candidates)
		{
			if (candidate.Length < minimum || candidate.Length > maximum) continue;

			match = candidate;
			return true;
		}

		match = string.Empty;
		return false;
	}

	private static bool MatchesKnownFormat(string candidate, string format)
	{
		var lowered = format.ToLowerInvariant();
		return lowered switch
		{
			"email" or "idn-email" => candidate.Contains("@"),
			"uri" or "iri" => Uri.TryCreate(candidate, UriKind.Absolute, out _),
			"uri-reference" or "iri-reference" => Uri.TryCreate(candidate, UriKind.RelativeOrAbsolute, out _),
			"uuid" => Guid.TryParse(candidate, out _),
			"hostname" or "idn-hostname" => candidate.Contains('.'),
			"ipv4" => candidate.Count(x => x == '.') == 3,
			"ipv6" => candidate.Contains(':'),
			_ => true
		};
	}

	private static string GenerateFromRegexFragment(string pattern, int maxLength)
	{
		if (string.IsNullOrEmpty(pattern) || maxLength <= 0)
			return string.Empty;

		var sb = new StringBuilder(Math.Min(pattern.Length, maxLength));
		var i = 0;
		while (i < pattern.Length && sb.Length < maxLength)
		{
			var c = pattern[i];
			if (c is '^' or '$')
			{
				i++;
				continue;
			}

			var token = GenerateSingleToken(pattern, ref i, maxLength - sb.Length);
			var repeats = ReadQuantifier(pattern, ref i, token.Length == 0 ? 0 : (maxLength - sb.Length) / Math.Max(token.Length, 1));
			for (var j = 0; j < repeats && sb.Length + token.Length <= maxLength; j++)
			{
				sb.Append(token);
			}
		}

		return sb.ToString();
	}

	private static string GenerateSingleToken(string pattern, ref int index, int maxTokenLength)
	{
		if (index >= pattern.Length || maxTokenLength <= 0)
			return string.Empty;

		var c = pattern[index];
		if (c == '\\')
		{
			index++;
			if (index >= pattern.Length) return "\\";

			var escaped = pattern[index++];
			return escaped switch
			{
				'd' => RandomChar("0123456789").ToString(),
				'w' => RandomChar("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_").ToString(),
				's' => " ",
				'D' => RandomChar("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ").ToString(),
				'W' => RandomChar(".-/ ").ToString(),
				'S' => RandomChar("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789").ToString(),
				_ => escaped.ToString()
			};
		}

		if (c == '[')
		{
			index++;
			var characterClass = ReadCharacterClass(pattern, ref index);
			return characterClass.Length == 0 ? string.Empty : RandomChar(characterClass).ToString();
		}

		if (c == '(')
		{
			var group = ReadGroup(pattern, ref index);
			var alternatives = SplitTopLevelAlternatives(group);
			var chosen = JsonSchemaExtensions.Randomizer.ArrayElement(alternatives.ToArray());
			return GenerateFromRegexFragment(chosen, maxTokenLength);
		}

		if (c == '.')
		{
			index++;
			return RandomChar(_fallbackAlphabet).ToString();
		}

		index++;
		return c.ToString();
	}

	private static int ReadQuantifier(string pattern, ref int index, int maxRepeats)
	{
		if (maxRepeats <= 0)
			return 0;
		if (index >= pattern.Length)
			return 1;

		var c = pattern[index];
		int min;
		int max;
		switch (c)
		{
			case '*':
				index++;
				min = 0;
				max = 3;
				break;
			case '+':
				index++;
				min = 1;
				max = 3;
				break;
			case '?':
				index++;
				min = 0;
				max = 1;
				break;
			case '{':
				(index, min, max) = ParseExplicitQuantifier(pattern, index);
				break;
			default:
				return 1;
		}

		max = Math.Min(max, Math.Max(min, maxRepeats));
		return JsonSchemaExtensions.Randomizer.Int(min, max);
	}

	private static (int NextIndex, int Min, int Max) ParseExplicitQuantifier(string pattern, int index)
	{
		var closeBrace = pattern.IndexOf('}', index);
		if (closeBrace == -1) return (index, 1, 1);

		var content = pattern.Substring(index + 1, closeBrace - index - 1);
		var parts = content.Split(',');
		if (!int.TryParse(parts[0], out var min)) return (closeBrace + 1, 1, 1);

		int max;
		if (parts.Length == 1)
			max = min;
		else if (string.IsNullOrWhiteSpace(parts[1]))
			max = min + 5;
		else if (!int.TryParse(parts[1], out max))
			max = min;

		if (max < min) max = min;
		return (closeBrace + 1, min, max);
	}

	private static string ReadCharacterClass(string pattern, ref int index)
	{
		var builder = new StringBuilder();
		var isNegated = index < pattern.Length && pattern[index] == '^';
		if (isNegated) index++;

		while (index < pattern.Length && pattern[index] != ']')
		{
			if (index + 2 < pattern.Length && pattern[index + 1] == '-' && pattern[index + 2] != ']')
			{
				var start = pattern[index];
				var end = pattern[index + 2];
				if (start <= end)
				{
					for (var c = start; c <= end; c++)
						builder.Append(c);
				}
				index += 3;
				continue;
			}

			if (pattern[index] == '\\' && index + 1 < pattern.Length)
			{
				index++;
				builder.Append(pattern[index]);
				index++;
				continue;
			}

			builder.Append(pattern[index]);
			index++;
		}

		if (index < pattern.Length && pattern[index] == ']')
			index++;

		var characters = builder.Length == 0 ? _fallbackAlphabet : builder.ToString();
		if (!isNegated) return characters;

		var negated = new string(_fallbackAlphabet.Where(c => !characters.Contains(c)).ToArray());
		return string.IsNullOrEmpty(negated) ? _fallbackAlphabet : negated;
	}

	private static string ReadGroup(string pattern, ref int index)
	{
		index++; // (
		if (index < pattern.Length - 1 && pattern[index] == '?' && pattern[index + 1] == ':')
			index += 2;

		var depth = 1;
		var start = index;
		var inCharacterClass = false;
		while (index < pattern.Length)
		{
			var c = pattern[index];
			if (c == '\\')
			{
				index += 2;
				continue;
			}

			if (c == '[')
				inCharacterClass = true;
			else if (c == ']')
				inCharacterClass = false;

			if (!inCharacterClass)
			{
				if (c == '(') depth++;
				if (c == ')')
				{
					depth--;
					if (depth == 0)
					{
						var groupContent = pattern.Substring(start, index - start);
						index++;
						return groupContent;
					}
				}
			}

			index++;
		}

		return pattern.Substring(start);
	}

	private static IEnumerable<string> SplitTopLevelAlternatives(string pattern)
	{
		if (string.IsNullOrEmpty(pattern)) return [string.Empty];

		var parts = new List<string>();
		var depth = 0;
		var inCharacterClass = false;
		var escaped = false;
		var segmentStart = 0;

		for (var i = 0; i < pattern.Length; i++)
		{
			var c = pattern[i];
			if (escaped)
			{
				escaped = false;
				continue;
			}

			if (c == '\\')
			{
				escaped = true;
				continue;
			}

			if (c == '[')
				inCharacterClass = true;
			else if (c == ']')
				inCharacterClass = false;

			if (inCharacterClass)
				continue;

			if (c == '(') depth++;
			else if (c == ')') depth--;
			else if (c == '|' && depth == 0)
			{
				parts.Add(pattern.Substring(segmentStart, i - segmentStart));
				segmentStart = i + 1;
			}
		}

		parts.Add(pattern.Substring(segmentStart));
		return parts;
	}

	private static bool IsAnchored(string pattern)
	{
		if (string.IsNullOrEmpty(pattern)) return false;
		return pattern[0] == '^' && pattern[^1] == '$';
	}

	private static char RandomChar(string chars)
	{
		if (string.IsNullOrEmpty(chars)) return 'a';
		return chars[JsonSchemaExtensions.Randomizer.Int(0, chars.Length - 1)];
	}
}
