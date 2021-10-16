﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;

namespace Json.JmesPath
{
	/// <summary>
	/// Represents a JSON Path.
	/// </summary>
	public class JmesPath
	{
		private delegate bool TryParseMethod(ReadOnlySpan<char> span, ref int i, [NotNullWhen(true)] out IIndexExpression? index);

		private static readonly List<TryParseMethod> _parseMethods =
			new List<TryParseMethod>
			{
				ItemQueryIndex.TryParse,
				PropertyNameIndex.TryParse,
				SliceIndex.TryParse,
				SimpleIndex.TryParse
			};
		private static readonly Dictionary<string, ISelector> _reservedWords =
			new Dictionary<string, ISelector>
			{
				["length"] = LengthSelector.Instance
			};

		private readonly IEnumerable<ISelector> _nodes;

		private JmesPath(IEnumerable<ISelector> nodes)
		{
			_nodes = nodes;
		}

		/// <summary>
		/// Parses a <see cref="JmesPath"/> from a string.
		/// </summary>
		/// <param name="source">The source string.</param>
		/// <returns>The parsed path.</returns>
		/// <exception cref="PathParseException">Thrown if a syntax error occurred.</exception>
		public static JmesPath Parse(string source)
		{
			var i = 0;
			var span = source.AsSpan();
			var nodes = new List<ISelector>();
			while (i < span.Length)
			{
				var node = span[i] switch
				{
					'@' => AddLocalRootNode(span, ref i),
					'.' => AddPropertyOrRecursive(span, ref i),
					'[' => AddIndex(span, ref i),
					_ => null
				};
				if (node == null)
				{
					if (IsValidToStartPropertyName(span[i]))
						node = AddPropertyOrRecursive(span, ref i);
				}

				if (node == null)
					throw new PathParseException(i, "Could not identify selector");

				if (node is ErrorSelector error)
					throw new PathParseException(i, error.ErrorMessage);

				nodes.Add(node);
			}

			if (!nodes.Any())
				throw new PathParseException(i, "No path found");

			return new JmesPath(nodes);
		}

		/// <summary>
		/// Attempts to parse a <see cref="JmesPath"/> from a string.
		/// </summary>
		/// <param name="source">The source string.</param>
		/// <param name="path">The resulting path.</param>
		/// <returns><code>true</code> if successful; otherwise <code>false</code>.</returns>
		public static bool TryParse(string source, [NotNullWhen(true)] out JmesPath? path)
		{
			var i = 0;
			var span = source.AsSpan();
			return TryParse(span, ref i, false, out path);
		}

		internal static bool TryParse(ReadOnlySpan<char> span, ref int i, bool allowTrailingContent, [NotNullWhen(true)] out JmesPath? path)
		{
			var nodes = new List<ISelector>();
			while (i < span.Length)
			{
				var node = span[i] switch
				{
					'@' => AddLocalRootNode(span, ref i),
					'.' => AddPropertyOrRecursive(span, ref i),
					'[' => AddIndex(span, ref i),
					_ => null
				};
				if (node == null)
				{
					if (IsValidToStartPropertyName(span[i]))
						node = AddPropertyOrRecursive(span, ref i);
				}

				if (node == null || node is ErrorSelector)
				{
					if (allowTrailingContent) break;
					path = null;
					return false;
				}

				nodes.Add(node);
			}

			if (!nodes.Any())
			{
				path = null;
				return false;
			}

			path = new JmesPath(nodes);
			return true;
		}

		private static ISelector AddLocalRootNode(ReadOnlySpan<char> span, ref int i)
		{
			i++;
			return new LocalNodeSelector();
		}

		private static ISelector AddPropertyOrRecursive(ReadOnlySpan<char> span, ref int i)
		{
			var slice = span[i..];
			var propertyNameLength = 0;
			while (propertyNameLength < slice.Length && IsValidForPropertyName(slice[propertyNameLength]))
			{
				propertyNameLength++;
			}

			var propertyName = slice[..propertyNameLength];
			i += propertyNameLength;
			return _reservedWords.TryGetValue(propertyName.ToString(), out var node)
				? node
				: new PropertySelector(propertyName.ToString());
		}

		private static bool IsValidForPropertyName(char ch)
		{
			return IsValidToStartPropertyName(ch) ||
			       ch.In('0'..('9' + 1)) ||
			       ch.In(0x80..0x10FFFF);
		}

		private static bool IsValidToStartPropertyName(char ch)
		{
			return ch.In('a'..('z' + 1)) ||
			       ch.In('A'..('Z' + 1)) ||
			       ch.In('_');
		}

		private static ISelector AddIndex(ReadOnlySpan<char> span, ref int i)
		{
			var slice = span[i..];
			// replace this with an actual index parser that returns null to handle spaces
			if (slice.StartsWith("[*]"))
			{
				i += 3;
				return new IndexSelector(null);
			}

			// consume [
			i++;
			var ch = ',';
			var indices = new List<IIndexExpression>();
			while (ch == ',')
			{
				span.ConsumeWhitespace(ref i);
				if (!ParseIndex(span, ref i, out var index))
					return new ErrorSelector("Error parsing path index value");

				indices.Add(index!);

				span.ConsumeWhitespace(ref i);
				if (i >= span.Length) break;

				ch = span[i];
				i++;
			}

			if (ch != ']')
				return new ErrorSelector("Expected ']' or ','"); 
			
			return new IndexSelector(indices);
		}

		private static bool ParseIndex(ReadOnlySpan<char> span, ref int i, out IIndexExpression? index)
		{
			foreach (var tryParse in _parseMethods)
			{
				var j = i;
				if (tryParse(span, ref j, out index))
				{
					i = j;
					return true;
				}

				if (j != -1)
				{
					i = j;
					index = null;
					return false;
				}
			}

			index = null;
			return false;
		}

		/// <summary>
		/// Evaluates the path against a JSON instance.
		/// </summary>
		/// <param name="root">The root of the JSON instance.</param>
		/// <returns>The results of the evaluation.</returns>
		public PathResult Evaluate(JsonElement root)
		{
			var context = new EvaluationContext(root);

			foreach (var node in _nodes)
			{
				node.Evaluate(context);
			}

			return context.BuildResult();
		}

		/// <summary>Returns a string that represents the current object.</summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return string.Concat(_nodes.Select(n => n.ToString()));
		}
	}
}
