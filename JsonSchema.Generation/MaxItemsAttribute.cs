﻿using System;
using System.Collections.Generic;
using System.Linq;
using Json.Schema.Generation.Intents;

namespace Json.Schema.Generation
{
	/// <summary>
	/// Applies an `maxItems` keyword.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class MaxItemsAttribute : Attribute, IAttributeHandler
	{
		/// <summary>
		/// The maximum number of items.
		/// </summary>
		public uint Value { get; }

		/// <summary>
		/// Creates a new <see cref="MaxItemsAttribute"/> instance.
		/// </summary>
		/// <param name="value">The value.</param>
		public MaxItemsAttribute(uint value)
		{
			Value = value;
		}

		void IAttributeHandler.AddConstraints(SchemaGeneratorContext context, IEnumerable<Attribute> attributes)
		{
			var attribute = attributes.OfType<MaxItemsAttribute>().FirstOrDefault();
			if (attribute == null) return;

			if (!context.Type.IsArray()) return;

			context.Intents.Add(new MaxItemsIntent(attribute.Value));
		}
	}
}