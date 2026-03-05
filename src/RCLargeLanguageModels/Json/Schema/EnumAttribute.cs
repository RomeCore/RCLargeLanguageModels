using System;

namespace RCLargeLanguageModels.Json.Schema
{
	/// <summary>
	/// Represents an attribute that indicates a member is an enumeration and specifies enum values.
	/// </summary>
	public sealed class EnumAttribute : Attribute
	{
		public object[] Values { get; }

		public EnumAttribute(object[] values)
		{
			Values = values;
		}
	}
}