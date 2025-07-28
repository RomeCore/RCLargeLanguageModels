using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace RCLargeLanguageModels.Metadata
{
	/// <summary>
	/// Represents a dynamic metadata object that can store additional properties at runtime.
	/// </summary>
	public interface IAdditionalMetadata : IMetadata, IDynamicMetaObjectProvider
	{
	}

	public static partial class MetadataExtensions
	{
		public static IAdditionalMetadata? TryGetAdditionalMetadata(this IMetadataProvider provider)
		{
			return provider.Metadata.TryGet<IAdditionalMetadata>();
		}
	}
}