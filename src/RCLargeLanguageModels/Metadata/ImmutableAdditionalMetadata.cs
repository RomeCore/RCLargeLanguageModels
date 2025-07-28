using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Dynamic;
using System.Text;

namespace RCLargeLanguageModels.Metadata
{
	/// <summary>
	/// Represents an immutable dynamic metadata object.
	/// </summary>
	public class ImmutableAdditionalMetadata : DynamicObject, IAdditionalMetadata
	{
		private readonly ImmutableDictionary<string, object> _metadata;

		/// <summary>
		/// Gets a shared immutable empty instance of additional metadata object.
		/// </summary>
		public static ImmutableAdditionalMetadata Empty { get; } = new ImmutableAdditionalMetadata();

		/// <summary>
		/// Initializes a new instance of the <see cref="ImmutableAdditionalMetadata"/> class.
		/// </summary>
		public ImmutableAdditionalMetadata()
		{
			_metadata = ImmutableDictionary<string, object>.Empty;
		}

		/// <summary>
		/// Initializes a new instance with initial key-value pair.
		/// </summary>
		public ImmutableAdditionalMetadata(string key, object value)
		{
			_metadata = ImmutableDictionary<string, object>.Empty.SetItem(key, value);
		}

		/// <summary>
		/// Initializes a new instance with dictionary of initial values.
		/// </summary>
		public ImmutableAdditionalMetadata(IDictionary<string, object> initialValues)
		{
			_metadata = initialValues?.ToImmutableDictionary()
				?? ImmutableDictionary<string, object>.Empty;
		}

		/// <summary>
		/// Initializes a new instance with immutable dictionary of initial values.
		/// </summary>
		public ImmutableAdditionalMetadata(ImmutableDictionary<string, object> initialValues)
		{
			_metadata = initialValues;
		}

		/// <summary>
		/// Creates a new instance with added or updated metadata.
		/// </summary>
		public ImmutableAdditionalMetadata With(string key, object value)
		{
			return new ImmutableAdditionalMetadata(_metadata.SetItem(key, value));
		}

		/// <summary>
		/// Creates a new instance with removed metadata.
		/// </summary>
		public ImmutableAdditionalMetadata Without(string key)
		{
			return new ImmutableAdditionalMetadata(_metadata.Remove(key));
		}

		/// <summary>
		/// Gets the value associated with the specified key.
		/// </summary>
		public object this[string key] => _metadata.TryGetValue(key, out var value) ? value : null;

		/// <summary>
		/// Gets the value associated with the specified key.
		/// </summary>
		public T Get<T>(string key)
		{
			if (_metadata.TryGetValue(key, out var value))
			{
				try
				{
					return (T)Convert.ChangeType(value, typeof(T));
				}
				catch
				{
					return default;
				}
			}
			return default;
		}

		/// <summary>
		/// Tries to get the value associated with the specified key.
		/// </summary>
		public bool TryGet<T>(string key, out T value)
		{
			if (_metadata.TryGetValue(key, out var objValue))
			{
				try
				{
					value = (T)Convert.ChangeType(objValue, typeof(T));
					return true;
				}
				catch
				{
					value = default;
					return false;
				}
			}
			value = default;
			return false;
		}

		/// <summary>
		/// Determines whether the metadata contains the specified key.
		/// </summary>
		public bool ContainsKey(string key) => _metadata.ContainsKey(key);

		/// <summary>
		/// Gets all the keys in the metadata.
		/// </summary>
		public IEnumerable<string> Keys => _metadata.Keys;

		/// <summary>
		/// Gets all the values in the metadata.
		/// </summary>
		public IEnumerable<object> Values => _metadata.Values;

		/// <summary>
		/// Gets the number of metadata items.
		/// </summary>
		public int Count => _metadata.Count;

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			return _metadata.TryGetValue(binder.Name, out result);
		}

		public override IEnumerable<string> GetDynamicMemberNames()
		{
			return _metadata.Keys;
		}
	}
}