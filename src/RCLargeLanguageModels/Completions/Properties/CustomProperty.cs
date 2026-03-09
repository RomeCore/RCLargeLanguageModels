using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Completions.Properties
{
	/// <summary>
	/// Represents a custom completion property.
	/// </summary>
	public class CustomProperty : CompletionProperty
	{
		public override string Name { get; }

		public CustomProperty(string name, object value) : base(value)
		{
			Name = name;
		}
	}

	/// <summary>
	/// Represents a custom completion property with a specific type.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class CustomProperty<T> : CompletionProperty<T>
	{
		public override string Name { get; }

		public CustomProperty(string name, T value) : base(value)
		{
			Name = name;
		}
	}
}