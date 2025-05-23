﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.Schema.Generation.Intents;

namespace Json.Schema.Generation.Generators;

internal class ObjectSchemaGenerator : ISchemaGenerator
{
	public bool Handles(Type type)
	{
		return true;
	}

	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
#pragma warning disable IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
	public void AddConstraints(SchemaGenerationContextBase context)
#pragma warning restore IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
	{
		if (context is not TypeGenerationContext typeContext) return;

		if (typeContext.Type == typeof(object)) return;

		typeContext.Intents.Add(new TypeIntent(SchemaValueType.Object));

		var props = new Dictionary<string, MemberGenerationContext>();
		var required = new List<string>();
		var propertiesToGenerate = typeContext.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
		var fieldsToGenerate = typeContext.Type.GetFields(BindingFlags.Public | BindingFlags.Instance);
		var hiddenPropertiesToGenerate = typeContext.Type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance)
			.Where(p => p.GetCustomAttribute<JsonIncludeAttribute>() != null);
		var hiddenFieldsToGenerate = typeContext.Type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
			.Where(p => p.GetCustomAttribute<JsonIncludeAttribute>() != null);
		var membersToGenerate = propertiesToGenerate.Cast<MemberInfo>()
			.Concat(fieldsToGenerate)
			.Concat(hiddenPropertiesToGenerate)
			.Concat(hiddenFieldsToGenerate)
			.ToList();

		membersToGenerate = SchemaGeneratorConfiguration.Current.PropertyOrder switch
		{
			PropertyOrder.AsDeclared => [.. membersToGenerate.OrderBy(m => m, typeContext.DeclarationOrderComparer)],
			PropertyOrder.ByName => [.. membersToGenerate.OrderBy(m => m.Name)],
			_ => membersToGenerate
		};

		var conditionalAttributes = new Dictionary<object, List<(MemberInfo, ConditionalAttribute)>>();
		var strictPropertyDefinitions = new Dictionary<(object, string), MemberGenerationContext>();
		var addUnevaluatedProperties = false;

		foreach (var member in membersToGenerate)
		{
			var memberAttributes = member.GetCustomAttributes().ToList();
			var ignoreAttribute = (Attribute?)memberAttributes.OfType<JsonIgnoreAttribute>().FirstOrDefault(a=>a.Condition == JsonIgnoreCondition.Always) ??
								  memberAttributes.OfType<JsonExcludeAttribute>().FirstOrDefault();
			if (ignoreAttribute != null) continue;

			if (!memberAttributes.OfType<DescriptionAttribute>().Any())
			{
				var comments = SchemaGeneratorConfiguration.Current.XmlReader.GetMemberComments(member);
				if (!string.IsNullOrWhiteSpace(comments.Summary)) 
					memberAttributes.Add(new DescriptionAttribute(comments.Summary!));
			}

			var unconditionalAttributes = memberAttributes.Where(x => x is not ConditionalAttribute sga || sga.ConditionGroup == null).ToList();
			var localConditionalAttributes = memberAttributes.Except(unconditionalAttributes).OfType<ConditionalAttribute>().ToList();
			foreach (var conditions in localConditionalAttributes.GroupBy(x => x.ConditionGroup))
			{
				if (!conditionalAttributes.TryGetValue(conditions.Key!, out var list)) 
					conditionalAttributes[conditions.Key!] = list = [];

				list.AddRange(conditions.Select(x => (member, x)));
			}

			if (member.IsReadOnly() && !unconditionalAttributes.OfType<ReadOnlyAttribute>().Any())
				unconditionalAttributes.Add(new ReadOnlyAttribute(true));

			if (member.IsWriteOnly() && !unconditionalAttributes.OfType<WriteOnlyAttribute>().Any())
				unconditionalAttributes.Add(new WriteOnlyAttribute(true));

			var memberTypeContext = SchemaGenerationContextCache.Get(member.GetMemberType());
			var memberContext = new MemberGenerationContext(memberTypeContext, unconditionalAttributes, member.IsMarkedAsNullable());
			memberContext.GenerateIntents();

			var name = SchemaGeneratorConfiguration.Current.PropertyNameResolver!(member);
			var nameAttribute = unconditionalAttributes.OfType<JsonPropertyNameAttribute>().FirstOrDefault();
			if (nameAttribute != null)
				name = nameAttribute.Name;

			if (unconditionalAttributes.OfType<ObsoleteAttribute>().Any())
			{
				memberContext.Intents.Add(new DeprecatedIntent(true));
			}

			if (SchemaGeneratorConfiguration.Current.StrictConditionals &&
			    localConditionalAttributes.Count != 0)
			{
				addUnevaluatedProperties = true;
				var applicableConditionGroups = localConditionalAttributes.Select(x => x.ConditionGroup).Distinct();
				foreach (var conditionGroup in applicableConditionGroups)
				{
					strictPropertyDefinitions.Add((conditionGroup!, name), memberContext);
				}
			}
			else
				props.Add(name, memberContext);

			if (unconditionalAttributes.OfType<RequiredAttribute>().Any()
#if NET7_0_OR_GREATER
			    || unconditionalAttributes.OfType<System.Runtime.CompilerServices.RequiredMemberAttribute>().Any()
#endif
			   )
				required.Add(name);

			foreach (var conditionalRequiredAttribute in localConditionalAttributes.OfType<RequiredAttribute>())
			{
				conditionalRequiredAttribute.PropertyName = name;
			}
		}

		if (props.Count > 0)
		{
			context.Intents.Add(new PropertiesIntent(props.ToDictionary(x => x.Key, SchemaGenerationContextBase (x) => x.Value)));

			if (required.Count > 0)
				context.Intents.Add(new RequiredIntent(required));
		}

		var conditionGroups = context.Type.GetCustomAttributes()
			.OfType<IConditionalAttribute>()
			.SelectMany(x => ExpandEnumConditions(x, membersToGenerate))
			.GroupBy(x => x.Attribute.ConditionGroup)
			.ToList();

		if (conditionGroups.Count == 0) return;

		if (conditionGroups.Count == 1)
		{
			// add directly to schema
			var conditionKey = conditionGroups[0].Key!;
			if (conditionalAttributes.TryGetValue(conditionKey, out var consequences))
			{
				var strictProperties = strictPropertyDefinitions.Where(x => Equals(x.Key.Item1, conditionKey))
					.ToDictionary(x => x.Key.Item2, x => x.Value);
				var thenSubschema = GenerateThen(consequences, strictProperties);

				if (thenSubschema != null)
				{
					context.Intents.Add(GenerateIf(conditionGroups[0]));
					context.Intents.Add(thenSubschema);
				}
			}
		}
		else
		{
			// wrap in allOf
			var allOf = new AllOfIntent();
			foreach (var conditionGroup in conditionGroups)
			{
				var conditionKey = conditionGroup.Key!;
				if (conditionalAttributes.TryGetValue(conditionKey, out var consequences))
				{
					var strictProperties = strictPropertyDefinitions.Where(x => Equals(x.Key.Item1, conditionKey))
						.ToDictionary(x => x.Key.Item2, x => x.Value);
					var thenSubschema = GenerateThen(consequences, strictProperties);

					if (thenSubschema != null)
						allOf.Subschemas.Add(
						[
							GenerateIf(conditionGroup),
							thenSubschema
						]);
				}
			}
			if (allOf.Subschemas.Count != 0)
				context.Intents.Add(allOf);
		}

		if (addUnevaluatedProperties)
			context.Intents.Add(new UnevaluatedPropertiesIntent());
	}

	[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	private static IEnumerable<(IConditionalAttribute Attribute, MemberInfo Member)> ExpandEnumConditions(IConditionalAttribute condition, IEnumerable<MemberInfo> members)
	{
		var member = members.FirstOrDefault(x => x.Name == condition.PropertyName);
		if (member == null) yield break;

		var memberType = member.GetMemberType();
		switch (condition)
		{
			case IfMinAttribute min:
				min.PropertyType = memberType;
				break;
			case IfMaxAttribute max:
				max.PropertyType = memberType;
				break;
			case IfEnumAttribute when !memberType.IsEnum:
				yield break;
			case IfEnumAttribute ifEnumAttribute:
				var values = Enum.GetValues(memberType);
				foreach (var value in values)
				{
					if (ifEnumAttribute.UseNumbers)
						yield return (new IfAttribute(ifEnumAttribute.PropertyName, (int)value, value), member);
					else
						yield return (new IfAttribute(ifEnumAttribute.PropertyName, value.ToString(), value), member);
				}

				yield break;
		}
		yield return (condition, member);
	}

	private static IfIntent GenerateIf(IEnumerable<(IConditionalAttribute Attribute, MemberInfo Member)> conditions)
	{
		static void AddPotentialValue(SchemaGenerationContextBase context, JsonNode? newValue)
		{
			var existingConst = context.Intents.OfType<ConstIntent>().SingleOrDefault();
			if (existingConst is not null)
			{
				context.Intents.Remove(existingConst);
				context.Intents.Add(new EnumIntent(existingConst.Value, newValue));
				return;
			}

			var existingEnum = context.Intents.OfType<EnumIntent>().SingleOrDefault();
			if (existingEnum is not null)
			{
				existingEnum.AddValue(newValue);
				return;
			}

			context.Intents.Add(new ConstIntent(newValue));
		}

		var properties = new Dictionary<string, SchemaGenerationContextBase>();
		var required = new List<string>();
		foreach (var condition in conditions)
		{
			var name = SchemaGeneratorConfiguration.Current.PropertyNameResolver!(condition.Member);
			if (!properties.TryGetValue(name, out var context))
				properties[name] = context = new AdHocGenerationContext();
			switch (condition.Attribute)
			{
				case IfAttribute ifAtt:
					AddPotentialValue(context, ifAtt.Value);
					break;
				case IfMinAttribute ifMin:
					var minIntent = ifMin.GetIntent();
					if (minIntent == null) continue;
					context.Intents.Add(minIntent);
					break;
				case IfMaxAttribute ifMax:
					var maxIntent = ifMax.GetIntent();
					if (maxIntent == null) continue;
					context.Intents.Add(maxIntent);
					break;
			}
			if (!required.Contains(name))
				required.Add(name);
		}

		var ifIntent = new IfIntent([
			new PropertiesIntent(properties),
			new RequiredIntent(required)
		]);

		return ifIntent;
	}

	private static ThenIntent? GenerateThen(List<(MemberInfo member, ConditionalAttribute attribute)> consequences,
		Dictionary<string, MemberGenerationContext> prebuiltMemberContexts)
	{
		var applicable = consequences.Where(x => x.attribute is IAttributeHandler); // should be all
		var required = consequences.Where(x => x.attribute is RequiredAttribute)
			.Select(x => ((RequiredAttribute)x.attribute).PropertyName)
			.ToList();
		var properties = prebuiltMemberContexts;
		foreach (var consequence in applicable.GroupBy(x => x.member))
		{
			var name = SchemaGeneratorConfiguration.Current.PropertyNameResolver!(consequence.Key);
			if (properties.TryGetValue(name, out var localContext))
				localContext = new MemberGenerationContext(localContext);
			else
			{
				var type = consequence.Key.GetMemberType();
				localContext = new MemberGenerationContext(SchemaGenerationContextCache.Get(type), []);
				localContext.GenerateIntents();
			}
			foreach (var (_, attribute) in consequence)
			{
				((IAttributeHandler)attribute).AddConstraints(localContext, attribute);
			}

			properties[name] = localContext;
		}

		var thenIntents = new List<ISchemaKeywordIntent>();

		if (properties.Count != 0)
			thenIntents.Add(new PropertiesIntent(properties.ToDictionary(x => x.Key, SchemaGenerationContextBase (x) => x.Value)));

		if (required.Count != 0)
			thenIntents.Add(new RequiredIntent(required));

		if (thenIntents.Count == 0) return null;

		var thenIntent = new ThenIntent(thenIntents);

		return thenIntent;
	}
}