using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Completions.Properties
{
	/// <summary>
	/// Represents a custom completion property.
	/// </summary>
	public class CustomCompletionProperty : CompletionProperty
	{
		public override string Name { get; }
		public override object RawValue { get; }

		public CustomCompletionProperty(string name, object value)
		{
			Name = name;
			RawValue = value;
		}
	}

	/// <summary>
	/// Represents a custom completion property with a specific type.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class CustomCompletionProperty<T> : CompletionProperty<T>
	{
		public override string Name { get; }
		public override T Value { get; }

		public CustomCompletionProperty(string name, T value)
		{
			Name = name;
			Value = value;
		}
	}
}