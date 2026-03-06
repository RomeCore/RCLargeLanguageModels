using System;

namespace RCLargeLanguageModels.Json.Schema
{
	/// <summary>
	/// Represents an attribute that indicates a member is an enumeration and specifies enum values.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Enum | AttributeTargets.Class)]
	public sealed class EnumAttribute : Attribute
	{
		public object[] Values { get; }

		public EnumAttribute(object[] values)
		{
			Values = values;
		}
	}
}