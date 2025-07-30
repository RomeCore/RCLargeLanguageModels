using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates.DataAccessors
{
	/// <summary>
	/// A boolean accessor for template data.
	/// </summary>
	public class TemplateBooleanAccessor : TemplateDataAccessor
	{
		/// <summary>
		/// Gets the value of the boolean accessor.
		/// </summary>
		public bool Value { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateBooleanAccessor"/> class using the specified boolean value.
		/// </summary>
		public TemplateBooleanAccessor(bool value)
		{
			Value = value;
		}

		public override bool AsBoolean()
		{
			return Value;
		}

		public override object GetValue()
		{
			return Value;
		}

		public override string ToString(string? format = null)
		{
			if (!string.IsNullOrEmpty(format))
			{
				var split = format.Split('/'); // Yes/No or True/False
				if (split.Length == 2)
					return Value ? split[0] : split[1];
				else
					throw new FormatException("Invalid format string for boolean accessor.");
			}
			return Value.ToString();
		}

		public override TemplateDataAccessor Operator(UnaryOperatorType type)
		{
			switch (type)
			{
				case UnaryOperatorType.Negate:
				case UnaryOperatorType.LogicalNot:
					return new TemplateBooleanAccessor(!AsBoolean());

				default:
					throw new TemplateRuntimeException("Invalid operator type.", dataAccessor: this);
			}
		}

		public override TemplateDataAccessor Operator(TemplateDataAccessor other, BinaryOperatorType type)
		{
			var booleanThis = Value;
			var booleanOther = other.AsBoolean();

			return type switch
			{
				BinaryOperatorType.LogicalAnd => new TemplateBooleanAccessor(booleanThis && booleanOther),
				BinaryOperatorType.LogicalOr => new TemplateBooleanAccessor(booleanThis || booleanOther),
				BinaryOperatorType.Equal => new TemplateBooleanAccessor(booleanThis == booleanOther),
				BinaryOperatorType.NotEqual => new TemplateBooleanAccessor(booleanThis != booleanOther),

				BinaryOperatorType.Add or
				BinaryOperatorType.Subtract or
				BinaryOperatorType.Multiply or
				BinaryOperatorType.Divide or
				BinaryOperatorType.Modulus =>
					throw new TemplateRuntimeException(
						$"Operator '{type}' cannot be applied to boolean values"),

				BinaryOperatorType.LessThan or
				BinaryOperatorType.LessThanOrEqual or
				BinaryOperatorType.GreaterThan or
				BinaryOperatorType.GreaterThanOrEqual =>
					throw new TemplateRuntimeException(
						$"Comparison operator '{type}' is not valid for boolean values"),

				_ => throw new TemplateRuntimeException(
						$"Unknown operator type: {type}")
			};
		}
	}
}