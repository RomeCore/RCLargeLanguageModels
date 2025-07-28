using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Tasks;

namespace RCLargeLanguageModels
{
	/// <summary>
	/// The marker exception used to mark <see cref="Task{TResult}"/> inside the <see cref="IEnumerable{T}"/> as finished.
	/// </summary>
	public class TaskEnumerableFinishedException : Exception
	{
	}

	public static class EnumerableExtensions
	{
		/// <summary>
		/// Casts to <see cref="ReadOnlyCollection{T}"/> or creates new.
		/// </summary>
		public static IReadOnlyList<T> AsReadOnlyList<T>(this IEnumerable<T> collection)
		{
			return collection as ReadOnlyCollection<T> ?? collection.ToList().AsReadOnly();
		}

		/// <summary>
		/// Casts to <see cref="ReadOnlyDictionary{TKey, TValue}"/> or creates new.
		/// </summary>
		public static IReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
		{
			return dictionary as ReadOnlyDictionary<TKey, TValue> ?? new ReadOnlyDictionary<TKey, TValue>(dictionary);
		}

		/// <summary>
		/// Checks if <see cref="IEnumerable"/> has elements.
		/// </summary>
		public static bool Any(this IEnumerable enumerable)
		{
			foreach (var e in enumerable)
				return true;
			return false;
		}

		/// <summary>
		/// Checks if <see cref="IEnumerable"/> is empty.
		/// </summary>
		public static bool Empty(this IEnumerable enumerable)
		{
			foreach (var e in enumerable)
				return false;
			return true;
		}

		/// <summary>
		/// Returns null if <see cref="IEnumerable"/> is null or empty.
		/// </summary>
		public static TEnum? ToNullIfEmpty<TEnum>(this TEnum? enumerable)
			where TEnum : class, IEnumerable
		{
			if (enumerable?.Any() ?? false)
				return enumerable;
			return default;
		}
		
		/// <summary>
		/// Returns <see langword="true"/> if <see cref="IEnumerable"/> is null or empty.
		/// </summary>
		public static bool IsNullOrEmpty(this IEnumerable? enumerable)
		{
			if (enumerable?.Any() ?? false)
				return false;
			return true;
		}

		/// <summary>
		/// Converts <see cref="IAsyncEnumerable{T}"/> to <see cref="IEnumerable{T}"/> with <see cref="Task{TResult}"/>
		/// for legacy consumption with <see langword="foreach"/> and <see langword="await"/>.
		/// </summary>
		/// <remarks>
		/// Finishes with faulted or cancelled task, but normally finishes with <see cref="TaskEnumerableFinishedException"/>.
		/// Each task must be waited before moving to next.
		/// </remarks>
		public static IEnumerable<Task<T>> GetTaskEnumerator<T>(
			this IAsyncEnumerable<T> source,
			CancellationToken cancellationToken = default)
		{
			return new TaskEnumerableWrapper<T>(source, cancellationToken);
		}

		/// <summary>
		/// Converts <see cref="IAsyncEnumerator{T}"/> to <see cref="IEnumerator{T}"/> with <see cref="Task{TResult}"/>.
		/// Useful for manual iteration.
		/// </summary>
		/// <remarks>
		/// Finishes with faulted or cancelled task, but normally finishes with <see cref="TaskEnumerableFinishedException"/>.
		/// Each task must be waited before moving to next.
		/// </remarks>
		public static IEnumerator<Task<T>> AsTaskEnumerator<T>(
			this IAsyncEnumerator<T> source)
		{
			return new TaskEnumeratorWrapper<T>(source);
		}

		private sealed class TaskEnumerableWrapper<T> : IEnumerable<Task<T>>
		{
			private readonly IAsyncEnumerable<T> _source;
			private readonly CancellationToken _ct;

			public TaskEnumerableWrapper(IAsyncEnumerable<T> source, CancellationToken ct)
			{
				_source = source;
				_ct = ct;
			}

			public IEnumerator<Task<T>> GetEnumerator() =>
				_source.GetAsyncEnumerator(_ct).AsTaskEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}

		private sealed class TaskEnumeratorWrapper<T> : IEnumerator<Task<T>>
		{
			private readonly IAsyncEnumerator<T> _source;
			private Task<T> _current;
			private bool _started;
			private bool _completed;

			public TaskEnumeratorWrapper(IAsyncEnumerator<T> source)
			{
				_source = source ?? throw new ArgumentNullException(nameof(source));
				_current = Task.FromException<T>(new InvalidOperationException("Enumeration not started."));
				_started = false;
				_completed = false;
			}

			public Task<T> Current
			{
				get
				{
					if (!_started)
						throw new InvalidOperationException("Enumeration not started.");
					if (_completed)
						throw new InvalidOperationException("Enumeration completed.");
					return _current;
				}
			}

			object IEnumerator.Current => Current;

			public bool MoveNext()
			{
				if (_completed)
					return false;

				if (_started && !_current.IsCompleted)
					throw new InvalidOperationException("Previous task must be awaited before moving next.");

				_started = true;
				_current = MoveNextCoreAsync();
				return !_completed;
			}

			private async Task<T> MoveNextCoreAsync()
			{
				try
				{
					if (!await _source.MoveNextAsync())
					{
						_completed = true;
						throw new TaskEnumerableFinishedException();
					}
					return _source.Current;
				}
				catch
				{
					_completed = true;
					throw;
				}
			}

			public void Reset()
			{
				throw new NotSupportedException();
			}

			public void Dispose()
			{
				// Fire-and-forget disposal to avoid blocking
				_ = _source.DisposeAsync().AsTask();
			}
		}

		/// <summary>
		/// Wraps <see cref="IEnumerator"/> into <see cref="IEnumerable"/>.
		/// </summary>
		public static IEnumerable Wrap(this IEnumerator enumerator)
		{
			return new EnumerableWrapper(() => enumerator);
		}

		/// <summary>
		/// Wraps <see cref="IEnumerator{T}"/> into <see cref="IEnumerable{T}"/>.
		/// </summary>
		public static IEnumerable<T> Wrap<T>(this IEnumerator<T> enumerator)
		{
			return new EnumerableWrapper<T>(() => enumerator);
		}

		/// <summary>
		/// Wraps <see cref="IEnumerable"/> into <see cref="IEnumerable"/> to prevent modifications via downcasting.
		/// </summary>
		public static IEnumerable Wrap(this IEnumerable enumerable)
		{
			return new EnumerableWrapper(enumerable.GetEnumerator);
		}

		/// <summary>
		/// Wraps <see cref="IEnumerable{T}"/> into <see cref="IEnumerable{T}"/> to prevent modifications via downcasting.
		/// </summary>
		public static IEnumerable<T> Wrap<T>(this IEnumerable<T> enumerable)
		{
			return new EnumerableWrapper<T>(enumerable.GetEnumerator);
		}

		private class EnumerableWrapper : IEnumerable
		{
			private readonly Func<IEnumerator> _factory;

			public EnumerableWrapper(Func<IEnumerator> factory)
			{
				_factory = factory;
			}

			public IEnumerator GetEnumerator()
			{
				return _factory.Invoke();
			}
		}

		private class EnumerableWrapper<T> : IEnumerable<T>
		{
			private readonly Func<IEnumerator<T>> _factory;

			public EnumerableWrapper(Func<IEnumerator<T>> factory)
			{
				_factory = factory;
			}

			public IEnumerator<T> GetEnumerator()
			{
				return _factory.Invoke();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		/// <summary>
		/// Creates an array that contains this value.
		/// </summary>
		public static T[] WrapIntoArray<T>(this T value)
		{
			return new T[] { value };
		}

		/// <summary>
		/// Creates an immutable array that contains this value.
		/// </summary>
		public static ImmutableArray<T> WrapIntoImmutableArray<T>(this T value)
		{
			return ImmutableArray.Create(value);
		}
	}
}