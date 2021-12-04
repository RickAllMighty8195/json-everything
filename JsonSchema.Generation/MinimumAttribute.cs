﻿using System;
using System.Collections.Generic;
using Json.Schema.Generation.Intents;

namespace Json.Schema.Generation
{
	/// <summary>
	/// Applies a `minimum` keyword.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class MinimumAttribute : Attribute, IAttributeHandler
	{
		/// <summary>
		/// The minimum.
		/// </summary>
		public decimal Value { get; }

		/// <summary>
		/// Creates a new <see cref="MinimumAttribute"/> instance.
		/// </summary>
		/// <param name="value">The value.</param>
		public MinimumAttribute(double value)
		{
			Value = Convert.ToDecimal(value);
		}

		IEnumerable<ISchemaKeywordIntent> IAttributeHandler.GetConstraints(SchemaGeneratorContext context)
		{
			if (!context.Type.IsNumber()) yield break;

			yield return new MinimumIntent(Value);
		}
	}
}