﻿using System;
using System.Collections.Generic;
using Json.Schema.Generation.Intents;

namespace Json.Schema.Generation
{
	/// <summary>
	/// Applies a `multipleOf` keyword.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class MultipleOfAttribute : Attribute, IAttributeHandler
	{
		/// <summary>
		/// The divisor.
		/// </summary>
		public decimal Value { get; }

		/// <summary>
		/// Creates a new <see cref="MultipleOfAttribute"/> instance.
		/// </summary>
		/// <param name="value">The value.</param>
		public MultipleOfAttribute(double value)
		{
			Value = (decimal) value;
		}

		IEnumerable<ISchemaKeywordIntent> IAttributeHandler.GetConstraints(SchemaGeneratorContext context)
		{
			if (!context.Type.IsNumber()) yield break;

			yield return new MultipleOfIntent(Value);
		}
	}
}