﻿using System;
using System.Collections.Generic;
using Json.Schema.Generation.Intents;

namespace Json.Schema.Generation
{
	/// <summary>
	/// Applies a `readOnly` keyword.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class ReadOnlyAttribute : Attribute, IAttributeHandler
	{
		/// <summary>
		/// Whether the property should be read-only.
		/// </summary>
		public bool Value { get; }

		/// <summary>
		/// Creates a new <see cref="ReadOnlyAttribute"/> instance with a value of `true`.
		/// </summary>
		public ReadOnlyAttribute() : this(true) { }

		/// <summary>
		/// Creates a new <see cref="ReadOnlyAttribute"/> instance.
		/// </summary>
		/// <param name="value">The value.</param>
		public ReadOnlyAttribute(bool value)
		{
			Value = value;
		}

		IEnumerable<ISchemaKeywordIntent> IAttributeHandler.GetConstraints(SchemaGeneratorContext context)
		{
			yield return new ReadOnlyIntent(Value);
		}
	}
}