using System.Linq;
using System.Text;

namespace Json.Schema.Generation.SourceGeneration.Emitters;

internal static class ConditionalSchemaEmitter
{
	public static void EmitConditionals(StringBuilder sb, TypeInfo type, string indent, SchemaEmissionContext context)
	{
		var conditionalsWithConsequences = type.Conditionals
			.Where(c => c.PropertyConsequences.Count > 0)
			.ToList();
		
		if (conditionalsWithConsequences.Count == 0) return;
		if (conditionalsWithConsequences.Count == 1)
		{
			var conditional = conditionalsWithConsequences[0];
			EmitIfClause(sb, conditional, indent);
			EmitThenClause(sb, conditional, type, indent, context);
		}
		else
		{
			sb.AppendLine();
			sb.Append($"{indent}.AllOf(");
			
			for (int i = 0; i < conditionalsWithConsequences.Count; i++)
			{
				var conditional = conditionalsWithConsequences[i];
				sb.AppendLine();
				sb.Append($"{indent}\tnew JsonSchemaBuilder()");
				
				EmitIfClause(sb, conditional, indent + "\t");
				EmitThenClause(sb, conditional, type, indent + "\t", context);
				
				if (i < conditionalsWithConsequences.Count - 1)
					sb.Append(",");
			}
			
			sb.AppendLine();
			sb.Append($"{indent})");
		}
	}

	private static void EmitIfClause(StringBuilder sb, ConditionalInfo conditional, string indent)
	{
		sb.AppendLine();
		sb.Append($"{indent}.If(new JsonSchemaBuilder()");

		if (conditional.Triggers.Count > 0)
		{
			sb.AppendLine();
			sb.Append($"{indent}\t.Properties(");
			sb.AppendLine();
			
			for (int i = 0; i < conditional.Triggers.Count; i++)
			{
				var trigger = conditional.Triggers[i];
				sb.Append($"{indent}\t\t(\"{CodeEmitterHelpers.EscapeString(trigger.PropertySchemaName)}\", ");
				EmitTriggerSchema(sb, trigger);
				sb.Append(")");
				
				if (i < conditional.Triggers.Count - 1)
					sb.Append(",");
				sb.AppendLine();
			}
			
			sb.Append($"{indent}\t)");
			
			sb.AppendLine();
			sb.Append($"{indent}\t.Required(");
			for (int i = 0; i < conditional.Triggers.Count; i++)
			{
				if (i > 0)
					sb.Append(", ");
				sb.Append($"\"{CodeEmitterHelpers.EscapeString(conditional.Triggers[i].PropertySchemaName)}\"");
			}
			sb.Append(")");
		}

		sb.AppendLine();
		sb.Append($"{indent})");
	}

	private static void EmitTriggerSchema(StringBuilder sb, ConditionalTrigger trigger)
	{
		switch (trigger.Type)
		{
			case ConditionalTriggerType.Equality:
				sb.Append($"new JsonSchemaBuilder().Const({trigger.ExpectedValue})");
				break;
			case ConditionalTriggerType.Minimum:
				var minKeyword = trigger.IsExclusive ? "ExclusiveMinimum" : "Minimum";
				sb.Append($"new JsonSchemaBuilder().{minKeyword}({trigger.NumericValue})");
				break;
			case ConditionalTriggerType.Maximum:
				var maxKeyword = trigger.IsExclusive ? "ExclusiveMaximum" : "Maximum";
				sb.Append($"new JsonSchemaBuilder().{maxKeyword}({trigger.NumericValue})");
				break;
			case ConditionalTriggerType.Enum:
				sb.Append($"new JsonSchemaBuilder().Const({trigger.ExpectedValue})");
				break;
		}
	}

	private static void EmitThenClause(StringBuilder sb, ConditionalInfo conditional, TypeInfo type, string indent, SchemaEmissionContext context)
	{
		if (conditional.PropertyConsequences.Count == 0) return;

		sb.AppendLine();
		sb.Append($"{indent}.Then(new JsonSchemaBuilder()");

		var propertiesWithAttributes = conditional.PropertyConsequences
			.Where(c => c.ConditionalAttributes.Count > 0 || c.IsConditionallyReadOnly || c.IsConditionallyWriteOnly)
			.ToList();

		if (propertiesWithAttributes.Count > 0)
		{
			sb.AppendLine();
			sb.Append($"{indent}\t.Properties(");
			sb.AppendLine();

			for (int i = 0; i < propertiesWithAttributes.Count; i++)
			{
				var prop = propertiesWithAttributes[i];
				sb.Append($"{indent}\t\t(\"{CodeEmitterHelpers.EscapeString(prop.PropertySchemaName)}\", ");

				if (type.StrictConditionals)
				{
					var fullProp = type.Properties.FirstOrDefault(p => p.SchemaName == prop.PropertySchemaName);
					if (fullProp != null)
					{
						PropertySchemaEmitter.EmitPropertySchema(sb, fullProp, indent + "\t\t", context);
						
						foreach (var attr in prop.ConditionalAttributes)
						{
							if (SchemaCodeEmitter.ShouldEmitBuiltInAttribute(attr))
							{
								sb.AppendLine();
								sb.Append($"{indent}\t\t\t");
								SchemaCodeEmitter.EmitAttributeConstraint(sb, attr);
							}
						}
						
						if (prop.IsConditionallyReadOnly)
						{
							sb.AppendLine();
							sb.Append($"{indent}\t\t\t.ReadOnly(true)");
						}
						if (prop.IsConditionallyWriteOnly)
						{
							sb.AppendLine();
							sb.Append($"{indent}\t\t\t.WriteOnly(true)");
						}
					}
					else
					{
						sb.Append("new JsonSchemaBuilder()");
						foreach (var attr in prop.ConditionalAttributes)
						{
							if (SchemaCodeEmitter.ShouldEmitBuiltInAttribute(attr))
							{
								sb.AppendLine();
								sb.Append($"{indent}\t\t\t");
								SchemaCodeEmitter.EmitAttributeConstraint(sb, attr);
							}
						}
					}
				}
				else
				{
					sb.Append("new JsonSchemaBuilder()");

					foreach (var attr in prop.ConditionalAttributes)
					{
						if (SchemaCodeEmitter.ShouldEmitBuiltInAttribute(attr))
						{
							sb.AppendLine();
							sb.Append($"{indent}\t\t\t");
							SchemaCodeEmitter.EmitAttributeConstraint(sb, attr);
						}
					}

					if (prop.IsConditionallyReadOnly)
					{
						sb.AppendLine();
						sb.Append($"{indent}\t\t\t.ReadOnly(true)");
					}
					if (prop.IsConditionallyWriteOnly)
					{
						sb.AppendLine();
						sb.Append($"{indent}\t\t\t.WriteOnly(true)");
					}
				}

				sb.Append(")");

				if (i < propertiesWithAttributes.Count - 1)
					sb.Append(",");
				sb.AppendLine();
			}

			sb.Append($"{indent}\t)");
		}

		var requiredProps = conditional.PropertyConsequences
			.Where(c => c.IsConditionallyRequired)
			.ToList();

		if (requiredProps.Count > 0)
		{
			sb.AppendLine();
			sb.Append($"{indent}\t.Required(");
			
			for (int i = 0; i < requiredProps.Count; i++)
			{
				if (i > 0)
					sb.Append(", ");
				sb.Append($"\"{CodeEmitterHelpers.EscapeString(requiredProps[i].PropertySchemaName)}\"");
			}
			
			sb.Append(")");
		}

		sb.AppendLine();
		sb.Append($"{indent})");
	}
}
