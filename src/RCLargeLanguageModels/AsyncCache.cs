using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;

namespace RCLargeLanguageModels
{
	/// <summary>
	/// A thread-safe asynchronous cache that stores items with optional sliding expiration.
	/// Items are automatically created using an async factory method when requested.
	/// </summary>
	/// <typeparam name="TKey">The type of keys used to identify cache items.</typeparam>
	/// <typeparam name="TValue">The type of values stored in the cache.</typeparam>
	public class AsyncCache<TKey, TValue> : IDisposable
	{
		private readonly ConcurrentDictionary<TKey, CacheItem> _storage;
		private readonly Func<TKey, CancellationToken, Task<TValue>> _factory;
		private readonly TimeSpan? _slidingExpiration;
		private readonly Timer? _cleanupTimer;

		/// <summary>
		/// Initializes a new instance of the <see cref="AsyncCache{TKey, TValue}"/> class.
		/// </summary>
		/// <param name="factory">Async factory method to create items when they're not found in cache.</param>
		/// <param name="slidingExpirationTime">
		/// Optional sliding expiration time. If null, items never expire automatically.
		/// </param>
		/// <param name="cleanupInterval">
		/// Optional interval for automatic cleanup of expired items. If null, no automatic cleanup occurs.
		/// </param>
		/// <param name="keyComparer">Optional key comparer used for internal dictionary.</param>
		/// <exception cref="ArgumentNullException">Thrown when factory is null.</exception>
		public AsyncCache(
			Func<TKey, Task<TValue>> factory,
			TimeSpan? slidingExpirationTime = null,
			TimeSpan? cleanupInterval = null,
			IEqualityComparer<TKey>? keyComparer = null)
		{
			if (factory == null)
				throw new ArgumentNullException(nameof(factory));
			_factory = (k, ct) => factory(k);
			_slidingExpiration = slidingExpirationTime;
			_storage = new ConcurrentDictionary<TKey, CacheItem>(keyComparer ?? EqualityComparer<TKey>.Default);

			if (cleanupInterval != null)
			{
				_cleanupTimer = new Timer(
					_ => RemoveExpiredItems(),
					null,
					cleanupInterval.Value,
					cleanupInterval.Value);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AsyncCache{TKey, TValue}"/> class.
		/// </summary>
		/// <param name="factory">Async factory method to create items when they're not found in cache.</param>
		/// <param name="slidingExpirationTime">
		/// Optional sliding expiration time. If null, items never expire automatically.
		/// </param>
		/// <param name="cleanupInterval">
		/// Optional interval for automatic cleanup of expired items. If null, no automatic cleanup occurs.
		/// </param>
		/// <param name="keyComparer">Optional key comparer used for internal dictionary.</param>
		/// <exception cref="ArgumentNullException">Thrown when factory is null.</exception>
		public AsyncCache(
			Func<TKey, CancellationToken, Task<TValue>> factory,
			TimeSpan? slidingExpirationTime = null,
			TimeSpan? cleanupInterval = null,
			IEqualityComparer<TKey>? keyComparer = null)
		{
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
			_slidingExpiration = slidingExpirationTime;
			_storage = new ConcurrentDictionary<TKey, CacheItem>(keyComparer ?? EqualityComparer<TKey>.Default);

			if (cleanupInterval != null)
			{
				_cleanupTimer = new Timer(
					_ => RemoveExpiredItems(),
					null,
					cleanupInterval.Value,
					cleanupInterval.Value);
			}
		}

		private class CacheItem
		{
			public TValue Value { get; }
			public DateTimeOffset LastAccess { get; set; }
			public Task<TValue>? PendingTask { get; set; }

			public CacheItem(TValue value)
			{
				Value = value;
				LastAccess = DateTimeOffset.Now;
			}

			public CacheItem(Task<TValue> pendingTask)
			{
				PendingTask = pendingTask;
				LastAccess = DateTimeOffset.Now;
			}
		}

		/// <summary>
		/// Gets a value from the cache asynchronously. If the value doesn't exist or has expired,
		/// it will be created using the async factory method.
		/// </summary>
		/// <param name="key">The key of the value to retrieve.</param>
		/// <param name="cancellationToken">Optional cancellation token.</param>
		/// <returns>A task that represents the asynchronous operation and contains the cached or newly created value.</returns>
		/// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
		public async Task<TValue> GetAsync(TKey key, CancellationToken cancellationToken = default)
		{
			if (key == null) throw new ArgumentNullException(nameof(key));

			// Check if item exists and is valid
			if (_storage.TryGetValue(key, out var existingItem))
			{
				// Handle pending task (if another request is already creating this item)
				if (existingItem.PendingTask != null)
				{
					return await existingItem.PendingTask.ConfigureAwait(false);
				}

				// Check if not expired
				if (_slidingExpiration == null || DateTimeOffset.Now - existingItem.LastAccess < _slidingExpiration)
				{
					existingItem.LastAccess = DateTimeOffset.Now;
					return existingItem.Value;
				}

				// Remove expired item
				_storage.TryRemove(key, out _);
			}

			// Create the async task first to prevent multiple concurrent creations
			var task = _factory(key, cancellationToken);
			var newItem = new CacheItem(task);
			_storage[key] = newItem;

			try
			{
				var result = await task.ConfigureAwait(false);

				// Replace the pending task with the actual value
				_storage[key] = new CacheItem(result);

				return result;
			}
			catch
			{
				// Remove failed item from cache
				_storage.TryRemove(key, out _);
				throw;
			}
		}

		/// <summary>
		/// Gets an item, while forcefully removes an existing item in cache.
		/// </summary>
		/// <param name="key">The key to get new item for.</param>
		/// <param name="cancellationToken">The cancellation token that can be used to cancel the operation.</param>
		/// <returns>The refreshed item task.</returns>
		public async Task<TValue> RefreshAsync(TKey key, CancellationToken cancellationToken = default)
		{
			_storage.TryRemove(key, out _);
			return await GetAsync(key, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Attempts to get a value from the cache without creating it if not found.
		/// </summary>
		/// <param name="key">The key of the value to retrieve.</param>
		/// <param name="value">When this method returns, contains the value if found; otherwise, the default value.</param>
		/// <returns><see langword="true"/> if the value was found; otherwise, <see langword="false"/>.</returns>
		public bool TryGet(TKey key, out TValue value)
		{
			if (_storage.TryGetValue(key, out var item) &&
				item.PendingTask == null && // Only return completed items
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
		/// Gets the number of items currently in the cache (excluding pending items).
		/// </summary>
		public int Count
		{
			get
			{
				var count = 0;
				foreach (var item in _storage.Values)
				{
					if (item.PendingTask == null) count++;
				}
				return count;
			}
		}

		~AsyncCache()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_cleanupTimer != null)
				{
					using var waitHandle = new ManualResetEvent(false);
					_cleanupTimer.Dispose(waitHandle);
					waitHandle.WaitOne();
				}
			}
		}
	}
}