﻿using System.Collections.Generic;
using System.Text.Json;
using Json.Pointer;

namespace Json.JmesPath
{
	internal class EvaluationContext
	{
		public JsonElement Root { get; }
		public List<PathMatch> Current { get; }

		internal EvaluationContext(in JsonElement root)
		{
			Root = root.Clone();
			Current = new List<PathMatch>{new PathMatch(root, JsonPointer.Empty)};
		}

		internal PathResult BuildResult()
		{
			return new PathResult(Current);
		}
	}
}