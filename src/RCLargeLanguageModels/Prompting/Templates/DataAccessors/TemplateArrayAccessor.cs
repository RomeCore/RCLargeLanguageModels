using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates.DataAccessors
{
	/// <summary>
	/// Represents an array accessor for template data.
	/// </summary>
	public class TemplateArrayAccessor : TemplateDataAccessor, IEnumerableTemplateDataAccessor
	{
		private readonly ImmutableArray<TemplateDataAccessor> _array;

		/// <summary>
		/// Gets the number of elements in the array.
		/// </summary>
		public override int Length => _array.Length;

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateArrayAccessor"/> class.
		/// </summary>
		/// <param name="array">The source array.</param>
		public TemplateArrayAccessor(IEnumerable<TemplateDataAccessor> array)
		{
			_array = array?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(array));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateArrayAccessor"/> class.
		/// </summary>
		/// <param name="array">The immutable array.</param>
		public TemplateArrayAccessor(ImmutableArray<TemplateDataAccessor> array)
		{
			_array = array.IsDefault ? ImmutableArray<TemplateDataAccessor>.Empty : array;
		}

		public override TemplateDataAccessor Index(TemplateDataAccessor index)
		{
			try
			{
				var i = Convert.ToInt32(index.GetValue());
				if (i >= 0 && i < _array.Length)
				{
					return _array[i];
				}
				throw new TemplateRuntimeException($"Index out of range: {index}, Length: {_array.Length}", dataAccessor: this);
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

		public override bool AsBoolean()
		{
			return _array.Length > 0;
		}

		public override object GetValue()
		{
			return _array.Select(a => a.GetValue()).ToArray();
		}

		public override string ToString(string? format = null)
		{
			throw new TemplateRuntimeException("Array accessor cannot be directly converted to a string.");
		}

		public override TemplateDataAccessor Operator(UnaryOperatorType type)
		{
			return type switch
			{
				UnaryOperatorType.LogicalNot => new TemplateBooleanAccessor(!AsBoolean()),

				_ => throw new TemplateRuntimeException(
					$"Unary operator '{type}' is not valid for array values",
					dataAccessor: this)
			};
		}

		public override TemplateDataAccessor Operator(TemplateDataAccessor other, BinaryOperatorType type)
		{
			return type switch
			{
				BinaryOperatorType.Add when other is TemplateArrayAccessor otherArray =>
					new TemplateArrayAccessor(_array.AddRange(otherArray._array)),
				BinaryOperatorType.Add =>
					throw new TemplateRuntimeException(
						$"Binary operator '{type}' is not valid for array values with non-array operand",
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
						$"Operator '{type}' cannot be applied to array values",
						dataAccessor: this),

				_ => throw new TemplateRuntimeException(
					$"Unknown operator type: {type}",
					dataAccessor: this)
			};
		}

		public override bool Equals(object? obj)
		{
			if (obj is not TemplateArrayAccessor other)
				return false;

			if (_array.Length != other._array.Length)
				return false;

			for (int i = 0; i < _array.Length; i++)
			{
				if (!Equals(_array[i].GetValue(), other._array[i].GetValue()))
					return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				foreach (var item in _array)
				{
					hash = hash * 23 + (item.GetValue()?.GetHashCode() ?? 0);
				}
				return hash;
			}
		}

		public IEnumerator<TemplateDataAccessor> GetEnumerator()
		{
			foreach (var item in _array)
				yield return item;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}