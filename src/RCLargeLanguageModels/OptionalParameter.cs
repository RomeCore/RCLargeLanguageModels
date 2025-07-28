using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using RCLargeLanguageModels.Utilities;

namespace RCLargeLanguageModels
{
	/// <summary>
	/// Represents an optional parameter override options.
	/// </summary>
	public enum ParameterOverrideOptions
	{
		/// <summary>
		/// Use the overriden value, the value that was passed to the method.
		/// </summary>
		UseOverride,

		/// <summary>
		/// Use the original value that stored in the method's class.
		/// </summary>
		UseOriginal,

		/// <summary>
		/// Use the default value, null for reference types for example.
		/// </summary>
		UseDefault,

		/// <summary>
		/// Use the combined value of the original value and the overriden value.
		/// </summary>
		/// <remarks>
		/// For values combination, use the CombinatorUtility.Combine
		/// (some values may not be supported, check in the documentation or by CombinatorUtility.Supported(typeof(T))) <para/>
		/// For example: <br/>
		/// if parameter type is a some type that supports (+) operator then the result will be the sum of the two values. <br/>
		/// if parameter type is a <see cref="IEnumerable"/>, the result will be the concatenated values of the two values. <br/>
		/// if parameter type is other type then the result will be the recursively combined value of the two values: <br/>
		/// <code>
		/// class a { int a; int b; }
		/// var a = new a { a = 1, b = 2 };
		/// var b = new a { a = 3, b = 4 };
		/// var combined = Combinator.Combine(a, b); // combined.a = 4, combined.b = 6)
		/// // or
		/// var arr_a = new[] { 1, 2 };
		/// var arr_b = new[] { 3, 4 };
		/// var combined = Combinator.Combine(arr_a, arr_b); // combined = [1, 2, 3, 4]
		/// </code>
		/// </remarks>
		Combine
	}

	/// <summary>
	/// Represents an optional parameter with an overriden value.
	/// </summary>
	/// <remarks>
	/// Can be converted implicitly from the value type.
	/// </remarks>
	/// <typeparam name="T">The value type.</typeparam>
	public class OptionalParameter<T>
	{
		/// <summary>
		/// Gets the overriden value of the parameter.
		/// </summary>
		public T Value { get; }

		/// <summary>
		/// Gets the override options for the parameter.
		/// </summary>
		public ParameterOverrideOptions OverrideOptions { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="OptionalParameter{T}"/> class using the <see cref="ParameterOverrideOptions.UseOverride"/> option.
		/// </summary>
		/// <param name="value">The value of the parameter.</param>
		public OptionalParameter(T value)
		{
			Value = value;
			OverrideOptions = ParameterOverrideOptions.UseOverride;
		}
		
		/// <summary>
		/// Creates a new instance of the <see cref="OptionalParameter{T}"/> class.
		/// </summary>
		/// <param name="overrideOptions">The override options for the parameter.</param>
		public OptionalParameter(ParameterOverrideOptions overrideOptions)
		{
			Value = default;

			if (!Enum.IsDefined(typeof(ParameterOverrideOptions), overrideOptions))
				throw new ArgumentException("Invalid override options.", nameof(overrideOptions));
			if (overrideOptions == ParameterOverrideOptions.UseOverride)
				throw new ArgumentException("Cannot use UseOverride with a default value.", nameof(overrideOptions));

			OverrideOptions = overrideOptions;
		}
		
		/// <summary>
		/// Creates a new instance of the <see cref="OptionalParameter{T}"/> class.
		/// </summary>
		/// <param name="value">The value of the parameter.</param>
		/// <param name="overrideOptions">The override options for the parameter.</param>
		public OptionalParameter(T value, ParameterOverrideOptions overrideOptions)
		{
			Value = value;

			if (!Enum.IsDefined(typeof(ParameterOverrideOptions), overrideOptions))
				throw new ArgumentException("Invalid override options.", nameof(overrideOptions));
			if (overrideOptions == ParameterOverrideOptions.UseDefault
				|| overrideOptions == ParameterOverrideOptions.UseOriginal)
				Value = default;
			if (overrideOptions == ParameterOverrideOptions.Combine && !CombinatorUtility.Supported(typeof(T)))
				throw new ArgumentException("Cannot use Combine with a type that does not support it.", nameof(overrideOptions));

			OverrideOptions = overrideOptions;
		}

		/// <summary>
		/// Creates a new instance of the <see cref="OptionalParameter{T}"/> class using the <see cref="ParameterOverrideOptions.UseOverride"/> option.
		/// </summary>
		/// <param name="value">The value of the parameter.</param>
		public static implicit operator OptionalParameter<T>(T value) => new OptionalParameter<T>(value);

		/// <summary>
		/// Creates a new instance of the <see cref="OptionalParameter{T}"/> class using the options.
		/// </summary>
		/// <param name="overrideOptions">The override options for the parameter.</param>
		public static implicit operator OptionalParameter<T>(ParameterOverrideOptions overrideOptions) => new OptionalParameter<T>(overrideOptions);
	}

	public static class OptionalParameterExtensions
	{
		/// <summary>
		/// Gets the effective value of the parameter.
		/// </summary>
		/// <param name="parameter">The parameter to get the value from. Can be null to use the original value.</param>
		/// <param name="originalValue">The original value stored in the method's class.</param>
		/// <returns>The effective value of the parameter based on <see cref="OptionalParameter{T}.OverrideOptions"/>.</returns>
		public static T GetValue<T>(this OptionalParameter<T> parameter, T originalValue)
		{
			if (parameter == null)
				return originalValue;

			switch (parameter.OverrideOptions)
			{
				case ParameterOverrideOptions.UseOverride:
					return parameter.Value;
				case ParameterOverrideOptions.UseOriginal:
					return originalValue;
				case ParameterOverrideOptions.UseDefault:
					return default;
				case ParameterOverrideOptions.Combine:
					return CombinatorUtility.Combine(originalValue, parameter.Value);
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}