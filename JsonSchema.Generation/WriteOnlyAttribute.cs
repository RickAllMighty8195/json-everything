﻿using System;
using System.Collections.Generic;
using Json.Schema.Generation.Intents;

namespace Json.Schema.Generation
{
	/// <summary>
	/// Applies a `writeOnly` keyword.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class WriteOnlyAttribute : Attribute, IAttributeHandler
	{
		/// <summary>
		/// Whether the property should be write-only.
		/// </summary>
		public bool Value { get; }

		/// <summary>
		/// Creates a new <see cref="WriteOnlyAttribute"/> instance with a value of `true`.
		/// </summary>
		public WriteOnlyAttribute() : this(true) { }

		/// <summary>
		/// Creates a new <see cref="WriteOnlyAttribute"/> instance.
		/// </summary>
		/// <param name="value">The value.</param>
		public WriteOnlyAttribute(bool value)
		{
			Value = value;
		}

		IEnumerable<ISchemaKeywordIntent> IAttributeHandler.GetConstraints(SchemaGeneratorContext context)
		{
			yield return new WriteOnlyIntent(Value);
		}
	}
}