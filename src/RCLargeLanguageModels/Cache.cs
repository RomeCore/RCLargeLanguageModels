using System;
using System.Collections.Concurrent;
using System.Threading;

namespace RCLargeLanguageModels
{
	/// <summary>
	/// A thread-safe cache that stores items with optional sliding expiration.
	/// Items are automatically created using a factory method when requested.
	/// </summary>
	/// <typeparam name="TKey">The type of keys used to identify cache items.</typeparam>
	/// <typeparam name="TValue">The type of values stored in the cache.</typeparam>
	public class Cache<TKey, TValue> : IDisposable
	{
		private readonly ConcurrentDictionary<TKey, CacheItem> _storage;
		private readonly Func<TKey, TValue> _factory;
		private readonly TimeSpan? _slidingExpiration;
		private readonly Timer? _cleanupTimer;

		/// <summary>
		/// Initializes a new instance of the <see cref="Cache{TKey, TValue}"/> class.
		/// </summary>
		/// <param name="factory">Factory method to create items when they're not found in cache.</param>
		/// <param name="slidingExpirationTime">
		/// Optional sliding expiration time. If <see langword="null"/>, items never expire automatically.
		/// </param>
		/// <param name="cleanupInterval">
		/// Optional interval for automatic cleanup of expired items. If null, no automatic cleanup occurs.
		/// </param>
		/// <exception cref="ArgumentNullException">Thrown when factory is null.</exception>
		public Cache(
			Func<TKey, TValue> factory,
			TimeSpan? slidingExpirationTime = null,
			TimeSpan? cleanupInterval = null)
		{
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
			_slidingExpiration = slidingExpirationTime;
			_storage = new ConcurrentDictionary<TKey, CacheItem>();

			if (cleanupInterval != null)
				_cleanupTimer = new Timer(_ => RemoveExpiredItems(),
					null,
					cleanupInterval.Value,
					cleanupInterval.Value);
		}

		private class CacheItem
		{
			public TValue Value { get; }
			public DateTimeOffset LastAccess { get; set; }

			public CacheItem(TValue value)
			{
				Value = value;
				LastAccess = DateTimeOffset.Now;
			}
		}

		/// <summary>
		/// Gets a value from the cache. If the value doesn't exist or has expired,
		/// it will be created using the factory method.
		/// </summary>
		/// <param name="key">The key of the value to retrieve.</param>
		/// <returns>The cached or newly created value.</returns>
		/// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
		public TValue Get(TKey key)
		{
			if (key == null) throw new ArgumentNullException(nameof(key));

			// Try to get existing item
			if (_storage.TryGetValue(key, out var item))
			{
				// Check if expired
				if (_slidingExpiration == null || DateTimeOffset.Now - item.LastAccess < _slidingExpiration)
				{
					item.LastAccess = DateTimeOffset.Now; // Renew sliding window
					return item.Value;
				}

				// Remove expired item
				_storage.TryRemove(key, out _);
			}

			var newValue = _factory(key);
			var newItem = new CacheItem(newValue);
			_storage[key] = newItem;

			return newValue;
		}

		/// <summary>
		/// Attempts to get a value from the cache without creating it if not found.
		/// </summary>
		/// <param name="key">The key of the value to retrieve.</param>
		/// <param name="value">When this method returns, contains the value if found; otherwise, the default value.</param>
		/// <returns>true if the value was found; otherwise, false.</returns>
		public bool TryGet(TKey key, out TValue value)
		{
			if (_storage.TryGetValue(key, out var item) &&
				(_slidingExpiration == null || DateTimeOffset.Now - item.LastAccess < _slidingExpiration))
			{
				item.LastAccess = DateTimeOffset.Now;
				value = item.Value;
				return true;
			}

			value = default;
			return false;
		}

		/// <summary>
		/// Removes all items that have exceeded their sliding expiration time.
		/// </summary>
		/// <remarks>
		/// Does nothing if sliding expiration time is not set.
		/// </remarks>
		public void RemoveExpiredItems()
		{
			if (_slidingExpiration == null)
				return;

			var now = DateTimeOffset.Now;
			foreach (var kvp in _storage)
			{
				if (now - kvp.Value.LastAccess >= _slidingExpiration)
				{
					_storage.TryRemove(kvp.Key, out _);
				}
			}
		}

		/// <summary>
		/// Removes all items from the cache.
		/// </summary>
		public void Clear() => _storage.Clear();

		/// <summary>
		/// Gets the number of items currently in the cache.
		/// </summary>
		public int Count => _storage.Count;

		/// <summary>
		/// Releases all resources used by this instance.
		/// </summary>
		public void Dispose()
		{
			_cleanupTimer?.Dispose();
			GC.SuppressFinalize(this);
		}
	}
}