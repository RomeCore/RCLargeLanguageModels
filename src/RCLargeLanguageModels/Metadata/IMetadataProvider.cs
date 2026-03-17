using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Metadata
{
	/// <summary>
	/// Represents an interface for objects that have metadata.
	/// </summary>
	public interface IMetadataProvider
	{
		/// <summary>
		/// Gets the metadata collection associated with this object.
		/// </summary>
		public IMetadataCollection Metadata { get; }
	}
}