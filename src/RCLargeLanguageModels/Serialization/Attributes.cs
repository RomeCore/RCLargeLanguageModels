using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Serialization
{
	/// <summary>
	/// 
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
	public class IdAttribute : Attribute
	{
		/// <summary>
		/// The ID value.
		/// </summary>
		public string Id { get; }

		/// <summary>
		/// Creates a new instance of <see cref="IdAttribute"/> attribute.
		/// </summary>
		/// <param name="id">The ID value.</param>
		public IdAttribute(string id)
		{
			Id = id;
		}
	}
}