using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace RCLargeLanguageModels.Prompting.Templates.DataAccessors
{
	/// <summary>
	/// Number accessor for template data.
	/// </summary>
	public class TemplateNumberAccessor : TemplateDataAccessor
	{
		/// <summary>
		/// Gets the numeric value of the template data.
		/// </summary>
		public double Value { get; }

		public TemplateNumberAccessor(double value)
		{
			Value = value;
		}

		public override bool AsBoolean()
		{
			return Value != 0;
		}

		public override object GetValue()
		{
			return Value;
		}

		public override string ToString(string? format = null)
		{
			return Value.ToString(format);
		}

		public override TemplateDataAccessor Operator(UnaryOperatorType type)
		{
			return type switch
			{
				UnaryOperatorType.Negate => new TemplateNumberAccessor(-Value),
				UnaryOperatorType.LogicalNot => new TemplateBooleanAccessor(!AsBoolean()),
				_ => throw new TemplateRuntimeException("Invalid operator type for number.", dataAccessor: this)
			};
		}

		public override TemplateDataAccessor Operator(TemplateDataAccessor other, BinaryOperatorType type)
		{
			double otherValue = other switch
			{
				TemplateNumberAccessor numberAccessor => numberAccessor.Value,
				_ => other.AsBoolean() ? 1 : 0 // Fallback to boolean conversion for non-number types
			};

			return type switch
			{
				BinaryOperatorType.Add => new TemplateNumberAccessor(Value + otherValue),
				BinaryOperatorType.Subtract => new TemplateNumberAccessor(Value - otherValue),
				BinaryOperatorType.Multiply => new TemplateNumberAccessor(Value * otherValue),
				BinaryOperatorType.Divide => new TemplateNumberAccessor(Value / otherValue),
				BinaryOperatorType.Modulus => new TemplateNumberAccessor(Value % otherValue),

				BinaryOperatorType.Equal => new TemplateBooleanAccessor(Value == otherValue),
				BinaryOperatorType.NotEqual => new TemplateBooleanAccessor(Value != otherValue),
				BinaryOperatorType.LessThan => new TemplateBooleanAccessor(Value < otherValue),
				BinaryOperatorType.LessThanOrEqual => new TemplateBooleanAccessor(Value <= otherValue),
				BinaryOperatorType.GreaterThan => new TemplateBooleanAccessor(Value > otherValue),
				BinaryOperatorType.GreaterThanOrEqual => new TemplateBooleanAccessor(Value >= otherValue),

				BinaryOperatorType.LogicalAnd => new TemplateBooleanAccessor(AsBoolean() && other.AsBoolean()),
				BinaryOperatorType.LogicalOr => new TemplateBooleanAccessor(AsBoolean() || other.AsBoolean()),

				_ => throw new TemplateRuntimeException($"Unknown operator type: {type}")
			};
		}
	}
}