using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace RCLargeLanguageModels.Prompting.Templates.DataAccessors
{
	/// <summary>
	/// Represents a data accessor for a dictionary of template data.
	/// </summary>
	public class TemplateDictionaryAccessor : TemplateDataAccessor
	{
		private ImmutableDictionary<string, TemplateDataAccessor> _dictionary;

		public override int Length => _dictionary.Count;

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateDictionaryAccessor"/> class.
		/// </summary>
		/// <param name="dictionary">The dictionary of template data accessors.</param>
		/// <exception cref="ArgumentNullException"></exception>
		public TemplateDictionaryAccessor(IDictionary<string, TemplateDataAccessor> dictionary)
		{
			_dictionary = dictionary?.ToImmutableDictionary() ?? throw new ArgumentNullException(nameof(dictionary));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateDictionaryAccessor"/> class.
		/// </summary>
		/// <param name="dictionary">The dictionary of template data accessors.</param>
		/// <exception cref="ArgumentNullException"></exception>
		public TemplateDictionaryAccessor(ImmutableDictionary<string, TemplateDataAccessor> dictionary)
		{
			_dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
		}

		public override TemplateDataAccessor Index(TemplateDataAccessor index)
		{
			return Property(index.ToString());
		}

		public override TemplateDataAccessor Property(string key)
		{
			if (_dictionary.TryGetValue(key, out var accessor))
				return accessor;
			return base.Property(key);
		}

		public override bool AsBoolean()
		{
			return _dictionary.Count > 0;
		}

		public override object GetValue()
		{
			return _dictionary.ToDictionary(kv => kv.Key, kv => kv.Value.GetValue());
		}

		public override string ToString(string? format = null)
		{
			throw new TemplateRuntimeException("Dictionary accessor cannot be converted to a string.");
		}

		public override TemplateDataAccessor Operator(UnaryOperatorType type)
		{
			return type switch
			{
				UnaryOperatorType.LogicalNot => new TemplateBooleanAccessor(!AsBoolean()),
				_ => throw new TemplateRuntimeException(
					$"Unary operator '{type}' is not valid for dictionary values",
					dataAccessor: this)
			};
		}

		public override TemplateDataAccessor Operator(TemplateDataAccessor other, BinaryOperatorType type)
		{
			return type switch
			{
				BinaryOperatorType.Add when other is TemplateDictionaryAccessor otherArray =>
					new TemplateDictionaryAccessor(_dictionary.AddRange(otherArray._dictionary)),
				BinaryOperatorType.Add =>
					throw new TemplateRuntimeException(
						$"Binary operator '{type}' is not valid for dictionary values with non-dictionary operand",
						dataAccessor: this),

				BinaryOperatorType.Equal => new TemplateBooleanAccessor(Equals(other)),
				BinaryOperatorType.NotEqual => new TemplateBooleanAccessor(!Equals(other)),

				BinaryOperatorType.LogicalAnd => new TemplateBooleanAccessor(AsBoolean() && other.AsBoolean()),
				BinaryOperatorType.LogicalOr => new TemplateBooleanAccessor(AsBoolean() || other.AsBoolean()),

				BinaryOperatorType.Subtract or
				BinaryOperatorType.Multiply or
				BinaryOperatorType.Divide or
				BinaryOperatorType.Modulus or
				BinaryOperatorType.LessThan or
				BinaryOperatorType.LessThanOrEqual or
				BinaryOperatorType.GreaterThan or
				BinaryOperatorType.GreaterThanOrEqual =>
					throw new TemplateRuntimeException(
						$"Operator '{type}' cannot be applied to dictionary values",
						dataAccessor: this),

				_ => throw new TemplateRuntimeException(
					$"Unknown operator type: {type}",
					dataAccessor: this)
			};
		}

		public override bool Equals(object? obj)
		{
			if (obj is not TemplateDictionaryAccessor other)
				return false;

			if (_dictionary.Count != other._dictionary.Count)
				return false;

			foreach (var kvp in _dictionary)
			{
				if (!other._dictionary.TryGetValue(kvp.Key, out var otherValue))
					return false;

				if (!Equals(kvp.Value.GetValue(), otherValue.GetValue()))
					return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 19;
				foreach (var kvp in _dictionary.OrderBy(kv => kv.Key))
				{
					hash = hash * 31 + kvp.Key.GetHashCode();
					hash = hash * 31 + (kvp.Value.GetValue()?.GetHashCode() ?? 0);
				}
				return hash;
			}
		}
	}
}