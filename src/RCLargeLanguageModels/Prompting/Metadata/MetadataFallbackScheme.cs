using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RCLargeLanguageModels.Metadata;

namespace RCLargeLanguageModels.Prompting.Metadata
{
	/// <summary>
	/// The base class for a metadata fallback scheme.
	/// </summary>
	public abstract class MetadataFallbackScheme
	{
		/// <summary>
		/// Gets the fallback metadata for a given metadata and a collection of available metadata.
		/// </summary>
		/// <param name="targetMetadata">The metadata for which a fallback is needed.</param>
		/// <param name="availableMetadata">
		/// A collection of available metadata to consider for fallback.
		/// Must not be null or empty.
		/// </param>
		/// <returns>
		/// The fallback metadata out of the provided collection that is most suitable as a
		/// fallback for the given metadata, or null if no suitable fallback can be found.
		/// If multiple candidates are equally suitable, any one may be returned.
		/// </returns>
		public IMetadata GetFallbackMetadata(IMetadata targetMetadata, IEnumerable<IMetadata> availableMetadata)
		{
			return GetFallbackMetadataCore(targetMetadata, availableMetadata);
		}

		/// <inheritdoc cref="GetFallbackMetadata(IMetadata, IEnumerable{IMetadata})"/>
		protected abstract IMetadata GetFallbackMetadataCore(IMetadata targetMetadata, IEnumerable<IMetadata> availableMetadata);
	}

	/// <summary>
	/// The base class for a metadata fallback scheme.
	/// </summary>
	public abstract class MetadataFallbackScheme<Metadata> : MetadataFallbackScheme
		where Metadata : IMetadata
	{
		protected sealed override IMetadata GetFallbackMetadataCore(IMetadata targetMetadata, IEnumerable<IMetadata> availableMetadata)
		{
			if (targetMetadata is not Metadata metadata)
				throw new InvalidCastException("The provided target metadata does not match the expected type.");
			var list = new List<Metadata>(availableMetadata.Select(m =>
			{
				if (m is Metadata m2)
					return m2;
				throw new InvalidCastException("The provided fallback metadata does not match the expected type.");
			}));

			return GetFallbackMetadataCore(metadata, list);
		}

		/// <inheritdoc cref="MetadataFallbackScheme.GetFallbackMetadata(IMetadata, IEnumerable{IMetadata})"/>
		protected abstract Metadata GetFallbackMetadataCore(Metadata targetMetadata, List<Metadata> availableMetadata);
	}
}