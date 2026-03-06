using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Completions
{
	/// <summary>
	/// Represents a base class for all completion properties.
	/// </summary>
	public abstract class CompletionProperty
	{
		/// <summary>
		/// Gets the JSON name of this completion property, for example "temperature" or "top_p".
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Gets the value of this completion property. The type depends on the specific implementation.
		/// </summary>
		public object RawValue { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CompletionProperty"/> class.
		/// </summary>
		/// <param name="value">The value of this completion property. The type depends on the specific implementation.</param>
		public CompletionProperty(object value)
		{
			RawValue = value;
		}
	}

	/// <summary>
	/// Represents a completion property with a specific type.
	/// </summary>
	/// <typeparam name="T">The type of the completion property.</typeparam>
	public abstract class CompletionProperty<T> : CompletionProperty
	{
		/// <summary>
		/// Gets or sets the value of this completion property.
		/// </summary>
		public T Value { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CompletionProperty"/> class.
		/// </summary>
		/// <param name="value">The value of this completion property. The type depends on the specific implementation.</param>
		public CompletionProperty(T value) : base(value)
		{
			Value = value;
		}
	}

	/// <summary>
	/// Represents a completion property with a specific type that is a floating-point number.
	/// </summary>
	public abstract class FloatCompletionProperty : CompletionProperty<float>
	{
		/// <summary>
		/// Gets the minimum value of this completion property.
		/// </summary>
		public abstract float MinValue { get; }

		/// <summary>
		/// Gets the maximum value of this completion property.
		/// </summary>
		public abstract float MaxValue { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="FloatCompletionProperty"/> class.
		/// </summary>
		/// <param name="value">The initial value of this completion property.</param>
		public FloatCompletionProperty(float value) : base(value)
		{
			if (value < MinValue || value > MaxValue)
				throw new ArgumentOutOfRangeException(nameof(value));
		}

		/// <summary>
		/// Converts the value of this completion property to a range between <paramref name="newMin"/> and <paramref name="newMax"/>.
		/// </summary>
		/// <param name="newMin">The minimum value of the new range.</param>
		/// <param name="newMax">The maximum value of the new range.</param>
		/// <returns>The value of this completion property in the new range.</returns>
		public float ToRange(float newMin, float newMax)
		{
			return (newMax - newMin) * (Value - MinValue) / (MaxValue - MinValue) + newMin;
		}
	}

	/// <summary>
	/// Represents a completion property with a specific type that is an integer number.
	/// </summary>
	public abstract class IntCompletionProperty : CompletionProperty<int>
	{
		/// <summary>
		/// Gets the minimum value of this completion property.
		/// </summary>
		public abstract int MinValue { get; }

		/// <summary>
		/// Gets the maximum value of this completion property.
		/// </summary>
		public abstract int MaxValue { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="IntCompletionProperty"/> class.
		/// </summary>
		/// <param name="value">The initial value of this completion property.</param>
		public IntCompletionProperty(int value) : base(value)
		{
			if (value < MinValue || value > MaxValue)
				throw new ArgumentOutOfRangeException(nameof(value));
		}
	}
}