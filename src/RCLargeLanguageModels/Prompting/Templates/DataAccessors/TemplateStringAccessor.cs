using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace RCLargeLanguageModels.Prompting.Templates.DataAccessors
{
	/// <summary>
	/// Represents a template data accessor for string values.
	/// </summary>
	public class TemplateStringAccessor : TemplateDataAccessor
	{
		public string Value { get; }
		public override int Length => Value.Length;

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateStringAccessor"/> class.
		/// </summary>
		/// <param name="value">The string value.</param>
		public TemplateStringAccessor(string? value)
		{
			Value = value ?? string.Empty;
		}

		public override bool AsBoolean()
		{
			return !string.IsNullOrEmpty(Value);
		}

		public override object GetValue()
		{
			return Value;
		}

		public override TemplateDataAccessor Index(TemplateDataAccessor index)
		{
			try
			{
				var i = Convert.ToInt32(index.GetValue());
				if (i >= 0 && i < Value.Length)
				{
					return new TemplateStringAccessor(Value[i].ToString());
				}
				throw new TemplateRuntimeException($"Index out of range: {index}, Length: {Value.Length}", dataAccessor: this);
			}
			catch (TemplateRuntimeException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new TemplateRuntimeException($"Failed to convert index to integer: {index.GetValue()}", dataAccessor: this, innerException: ex);
			}
		}

		public override string ToString(string? format = null)
		{
			switch (format)
			{
				case null:
					return Value;
				case "upper":
					return Value.ToUpper();
				case "lower":
					return Value.ToLower();
				case "trim":
					return Value.Trim();
				default:
					return Value;
			}
		}

		public override TemplateDataAccessor Operator(UnaryOperatorType type)
		{
			return type switch
			{
				UnaryOperatorType.LogicalNot => new TemplateBooleanAccessor(!AsBoolean()),
				
				_ => throw new TemplateRuntimeException(
					$"Unary operator '{type}' is not valid for string values",
					dataAccessor: this)
			};
		}

		public override TemplateDataAccessor Operator(TemplateDataAccessor other, BinaryOperatorType type)
		{
			string otherValue = other.ToString();

			return type switch
			{
				BinaryOperatorType.Add => new TemplateStringAccessor(Value + otherValue),

				BinaryOperatorType.Equal => new TemplateBooleanAccessor(Value == otherValue),
				BinaryOperatorType.NotEqual => new TemplateBooleanAccessor(Value != otherValue),

				BinaryOperatorType.LessThan => new TemplateBooleanAccessor(string.Compare(Value, otherValue) < 0),
				BinaryOperatorType.LessThanOrEqual => new TemplateBooleanAccessor(string.Compare(Value, otherValue) <= 0),
				BinaryOperatorType.GreaterThan => new TemplateBooleanAccessor(string.Compare(Value, otherValue) > 0),
				BinaryOperatorType.GreaterThanOrEqual => new TemplateBooleanAccessor(string.Compare(Value, otherValue) >= 0),

				BinaryOperatorType.LogicalAnd => new TemplateBooleanAccessor(AsBoolean() && other.AsBoolean()),
				BinaryOperatorType.LogicalOr => new TemplateBooleanAccessor(AsBoolean() || other.AsBoolean()),

				BinaryOperatorType.Subtract or
				BinaryOperatorType.Multiply or
				BinaryOperatorType.Divide or
				BinaryOperatorType.Modulus =>
					throw new TemplateRuntimeException(
						$"Arithmetic operator '{type}' cannot be applied to string values",
						dataAccessor: this),

				_ => throw new TemplateRuntimeException(
					$"Unknown operator type: {type}",
					dataAccessor: this)
			};
		}
	}
}