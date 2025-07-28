using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Tasks
{
	/// <summary>
	/// A thread-safe collection of CancellationTokenSource objects, each associated with a unique object key.
	/// </summary>
	public class CancellationTokenCollection : IDisposable
	{
		private readonly ConcurrentDictionary<object, CancellationTokenSource> _tokenSources = new ConcurrentDictionary<object, CancellationTokenSource>();

		/// <summary>
		/// Replaces or adds a CancellationTokenSource for the specified object key.
		/// If a token source already exists for the key, it is canceled and replaced.
		/// </summary>
		/// <param name="obj">The object key associated with the token source.</param>
		/// <returns>The CancellationToken of the new token source.</returns>
		public CancellationToken Replace(object obj)
		{
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));

			// Create a new CancellationTokenSource
			var newCts = new CancellationTokenSource();

			// If an existing token source exists, cancel and dispose of it
			if (_tokenSources.TryGetValue(obj, out var existingCts))
			{
				if (!existingCts.IsCancellationRequested)
					existingCts.Cancel();
				existingCts.Dispose();
			}

			// Add or replace the token source in the collection
			_tokenSources[obj] = newCts;

			return newCts.Token;
		}

		/// <summary>
		/// Cancels all token sources and clears the collection.
		/// </summary>
		public void Clear()
		{
			foreach (var cts in _tokenSources.Values)
			{
				if (!cts.IsCancellationRequested)
					cts.Cancel();
				cts.Dispose();
			}
			_tokenSources.Clear();
		}

		/// <summary>
		/// Removes and cancels the token source associated with the specified object key.
		/// </summary>
		/// <param name="obj">The object key to remove.</param>
		/// <returns>True if the token source was removed; otherwise, false.</returns>
		public bool Remove(object obj)
		{
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));

			if (_tokenSources.TryRemove(obj, out var cts))
			{
				if (!cts.IsCancellationRequested)
					cts.Cancel();
				cts.Dispose();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Checks if a token source exists for the specified object key.
		/// </summary>
		/// <param name="obj">The object key to check.</param>
		/// <returns>True if a token source exists; otherwise, false.</returns>
		public bool Contains(object obj)
		{
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));

			return _tokenSources.ContainsKey(obj);
		}

		/// <summary>
		/// Cancels all token sources without clearing the collection.
		/// </summary>
		public void CancelAll()
		{
			foreach (var cts in _tokenSources.Values)
			{
				if (!cts.IsCancellationRequested)
					cts.Cancel();
			}
		}

		/// <summary>
		/// Retrieves the CancellationToken for the specified object key.
		/// </summary>
		/// <param name="obj">The object key to retrieve the token for.</param>
		/// <returns>The CancellationToken if the key exists; otherwise, CancellationToken.None.</returns>
		public CancellationToken GetToken(object obj)
		{
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));

			if (_tokenSources.TryGetValue(obj, out var cts))
			{
				return cts.Token;
			}
			return CancellationToken.None;
		}

		/// <summary>
		/// Releases all resources used by the CancellationTokenCollection.
		/// </summary>
		public void Dispose()
		{
			Clear();
		}
	}
}