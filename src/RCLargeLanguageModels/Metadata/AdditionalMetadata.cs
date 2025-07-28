using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Dynamic;
using System.Text;

namespace RCLargeLanguageModels.Metadata
{
	/// <summary>
	/// Represents a dynamic metadata object that can store additional properties at runtime.
	/// </summary>
	public class AdditionalMetadata : DynamicObject, IAdditionalMetadata
	{
		private readonly Dictionary<string, object> _metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Gets a shared immutable empty instance of additional metadata object.
		/// </summary>
		public static IAdditionalMetadata Empty => ImmutableAdditionalMetadata.Empty;

		/// <summary>
		/// Gets or sets the value associated with the specified key.
		/// </summary>
		/// <param name="key">The key of the value to get or set.</param>
		/// <returns>The value associated with the specified key.</returns>
		public object this[string key]
		{
			get => _metadata.TryGetValue(key, out var value) ? value : null;
			set => _metadata[key] = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AdditionalMetadata"/> class.
		/// </summary>
		public AdditionalMetadata()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AdditionalMetadata"/> class with initial key-value pair.
		/// </summary>
		/// <param name="key">The key of the initial metadata item.</param>
		/// <param name="value">The value of the initial metadata item.</param>
		public AdditionalMetadata(string key, object value)
		{
			_metadata[key] = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AdditionalMetadata"/> class with dictionary of initial values.
		/// </summary>
		/// <param name="initialValues">Dictionary containing initial metadata values.</param>
		public AdditionalMetadata(IDictionary<string, object> initialValues)
		{
			if (initialValues != null)
			{
				foreach (var kvp in initialValues)
				{
					_metadata[kvp.Key] = kvp.Value;
				}
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AdditionalMetadata"/> class with collection of key-value pairs.
		/// </summary>
		/// <param name="initialValues">Collection of key-value pairs for initial metadata.</param>
		public AdditionalMetadata(IEnumerable<KeyValuePair<string, object>> initialValues)
		{
			if (initialValues != null)
			{
				foreach (var kvp in initialValues)
				{
					_metadata[kvp.Key] = kvp.Value;
				}
			}
		}

		/// <summary>
		/// Gets the value associated with the specified key.
		/// </summary>
		/// <typeparam name="T">The type to convert the value to.</typeparam>
		/// <param name="key">The key of the value to get.</param>
		/// <returns>The converted value or default(T) if not found.</returns>
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
		/// <typeparam name="T">The type to convert the value to.</typeparam>
		/// <param name="key">The key of the value to get.</param>
		/// <param name="value">When this method returns, contains the value if found; otherwise, default(T).</param>
		/// <returns>true if the key was found; otherwise, false.</returns>
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
		/// Adds or updates a metadata value.
		/// </summary>
		/// <param name="key">The key of the value to set.</param>
		/// <param name="value">The value to set.</param>
		public void Set(string key, object value)
		{
			_metadata[key] = value;
		}

		/// <summary>
		/// Determines whether the metadata contains the specified key.
		/// </summary>
		/// <param name="key">The key to locate.</param>
		/// <returns>true if the metadata contains the key; otherwise, false.</returns>
		public bool ContainsKey(string key)
		{
			return _metadata.ContainsKey(key);
		}

		/// <summary>
		/// Removes the value with the specified key.
		/// </summary>
		/// <param name="key">The key of the value to remove.</param>
		/// <returns>true if the element was found and removed; otherwise, false.</returns>
		public bool Remove(string key)
		{
			return _metadata.Remove(key);
		}

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

		/// <summary>
		/// Clears all metadata.
		/// </summary>
		public void Clear()
		{
			_metadata.Clear();
		}

		/// <summary>
		/// Converts this instance to immutable copy.
		/// </summary>
		public ImmutableAdditionalMetadata ToImmutable()
		{
			return new ImmutableAdditionalMetadata(_metadata);
		}

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			return _metadata.TryGetValue(binder.Name, out result);
		}

		public override bool TrySetMember(SetMemberBinder binder, object value)
		{
			_metadata[binder.Name] = value;
			return true;
		}

		public override IEnumerable<string> GetDynamicMemberNames()
		{
			return _metadata.Keys;
		}
	}
}