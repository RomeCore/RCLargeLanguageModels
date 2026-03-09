using System;

namespace RCLargeLanguageModels.Json.Schema
{
	/// <summary>
	/// Represents an attribute that can be used to specify the name of a JSON schema element.
	/// </summary>
	public sealed class NameAttribute : Attribute
	{
		public string Name { get; }

		public NameAttribute(string name)
		{
			Name = name;
		}
	}
}