using System;
using System.Collections;
using System.Collections.Generic;

namespace RCLargeLanguageModels.Metadata
{
	/// <summary>
	/// Represents a metadata collection with specified base metadata type.
	/// </summary>
	public class MetadataCollection : IMetadataCollection
	{
		private readonly ImmutableUniqueTypeDictionary<IMetadata> _dict;

		public int Count => _dict.Count;

		/// <summary>
		/// Gets an empty shared <see cref="MetadataCollection"/> instance.
		/// </summary>
		public static MetadataCollection Empty { get; } = new MetadataCollection();

		public MetadataCollection()
		{
			_dict = new ImmutableUniqueTypeDictionary<IMetadata>();
		}
		
		public MetadataCollection(IEnumerable<IMetadata> metadatas)
		{
			_dict = new ImmutableUniqueTypeDictionary<IMetadata>(metadatas);
		}
		
		public MetadataCollection(params IMetadata[] metadatas)
		{
			_dict = new ImmutableUniqueTypeDictionary<IMetadata>(metadatas);
		}

		public T? TryGet<T>()
			where T : IMetadata
		{
			return _dict.TryGet<T>();
		}

		public IEnumerable<T> GetAll<T>()
			where T : IMetadata
		{
			return _dict.GetAll<T>();
		}

		public bool Has<T>()
			where T : IMetadata
		{
			return _dict.Has<T>();
		}

		public T Require<T>(string exceptionMessage)
			where T : IMetadata
		{
			return _dict.Require<T>(exceptionMessage);
		}

		public bool Check<T>(Func<T, bool> predicate, bool fallback = false)
			where T : IMetadata
		{
			if (predicate is null)
				throw new ArgumentNullException(nameof(predicate));

			if (_dict.TryGet<T>() is T first)
			{
				try
				{
					return predicate.Invoke(first);
				}
				catch
				{
					return fallback;
				}
			}

			return fallback;
		}

		public IEnumerator<IMetadata> GetEnumerator()
		{
			return _dict.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _dict.GetEnumerator();
		}
	}
}