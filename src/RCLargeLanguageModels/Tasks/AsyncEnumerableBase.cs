using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace RCLargeLanguageModels.Tasks
{
	/// <summary>
	/// Provides a base class for creating replayable asynchronous enumerables that maintain all values.
	/// This implementation keeps all values in memory and allows multiple enumerations over the same data.
	/// </summary>
	/// <typeparam name="T">The type of elements to enumerate.</typeparam>
	/// <remarks>
	/// This class is thread-safe and suitable for scenarios where: <br/>
	/// 1. You need to enumerate values as they become available <br/>
	/// 2. You want to preserve all values for potential replay <br/>
	/// 3. You need to support multiple concurrent consumers <para/>
	/// See also: <see cref="AsyncEnumerable{T}"/> as implementation with public methods.
	/// </remarks>
	public class AsyncEnumerableBase<T> : IAsyncEnumerable<T>
	{
		private readonly List<T> _values;
		private readonly object _lock;
		private readonly AsyncSignal _signal;
		private readonly CompletionSource _cs;

		/// <summary>
		/// The completion token that gives info about completion fact and the completion state.
		/// </summary>
		protected CompletionToken CompletionToken => _cs.Token;

		/// <summary>
		/// Gets the vlue indicating whether enumerable is completed.
		/// </summary>
		protected bool IsCompleted => _cs.IsCompleted;

		/// <summary>
		/// Gets the read-only list containing current completed values.
		/// </summary>
		protected IReadOnlyList<T> Values { get; }

		/// <summary>
		/// Gets the count of currently completed values.
		/// </summary>
		protected int Count => _values.Count;

		private class AsyncEnumerator : IAsyncEnumerator<T>
		{
			private readonly AsyncEnumerableBase<T> _source;
			private readonly CancellationToken _ct;
			private int _index;

			public AsyncEnumerator(AsyncEnumerableBase<T> source, CancellationToken cancellationToken = default)
			{
				_source = source;
				_ct = cancellationToken;
				_index = -1;
			}

			public T Current
			{
				get
				{
					if (_index < 0)
						throw new InvalidOperationException("Enumeration is not started.");
					if (_index >= _source._values.Count)
						throw new InvalidOperationException("Enumeration is finished.");

					return _source._values[_index];
				}
			}

			public async ValueTask<bool> MoveNextAsync()
			{
				if (_ct.IsCancellationRequested)
					throw new TaskCanceledException();

				if (_index >= _source._values.Count && _source._cs.IsCompleted)
					return false;

				var next = _index + 1;

				if (next >= _source._values.Count)
				{
					await _source._signal.WaitAsync(_ct);

					if (next >= _source._values.Count)
					{
						_index = next;
						return false;
					}
				}

				_index = next;
				return true;
			}

			public ValueTask DisposeAsync()
			{
				return default;
			}
		}

		/// <summary>
		/// Initializes a new empty non-completed <see cref="AsyncEnumerableBase{T}"/>.
		/// </summary>
		public AsyncEnumerableBase()
		{
			_values = new List<T>();
			_signal = new AsyncSignal();
			_lock = new object();
			_cs = new CompletionSource(CompletionState.Incomplete);
		}

		/// <summary>
		/// Initializes a new non-completed <see cref="AsyncEnumerableBase{T}"/> with pre-populated values.
		/// </summary>
		/// <param name="completedValues">Values that are immediately available.</param>
		/// <exception cref="ArgumentNullException">Thrown if completedValues is null.</exception>
		public AsyncEnumerableBase(IEnumerable<T> completedValues)
		{
			if (completedValues == null)
				throw new ArgumentNullException(nameof(completedValues));

			_values = new List<T>(completedValues);
			_signal = new AsyncSignal();
			_lock = new object();
			_cs = new CompletionSource(CompletionState.Incomplete);
		}

		/// <summary>
		/// Initializes a new <see cref="AsyncEnumerableBase{T}"/> with pre-populated values and completion state.
		/// </summary>
		/// <param name="completedValues">Values that are immediately available.</param>
		/// <param name="isFinished">The initial state marking enumerable completion.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="completedValues"/> is null.</exception>
		public AsyncEnumerableBase(IEnumerable<T> completedValues, bool isFinished)
		{
			if (completedValues == null)
				throw new ArgumentNullException(nameof(completedValues));

			_values = new List<T>(completedValues);
			_signal = new AsyncSignal();
			_lock = new object();

			if (isFinished)
				_cs = new CompletionSource(CompletionState.Success);
			else
				_cs = new CompletionSource(CompletionState.Incomplete);
		}

		/// <summary>
		/// Initializes a new <see cref="AsyncEnumerableBase{T}"/> with pre-populated values and completion state with optional completion exception.
		/// </summary>
		/// <param name="completedValues">Values that are immediately available.</param>
		/// <param name="state">The initial state marking enumerable completion.</param>
		/// <param name="exception">
		/// The exception for <see cref="CompletionState.Failed"/> completion state.
		/// Gives info about what caused the enumerable to finish.
		/// </param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="completedValues"/> is null.</exception>
		public AsyncEnumerableBase(IEnumerable<T> completedValues, CompletionState state, Exception? exception = null)
		{
			if (completedValues == null)
				throw new ArgumentNullException(nameof(completedValues));

			_values = new List<T>(completedValues);
			Values = _values.AsReadOnly();
			_signal = new AsyncSignal();
			_lock = new object();
			_cs = new CompletionSource(state, exception);
		}

		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		{
			return new AsyncEnumerator(this, cancellationToken);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void CheckFinish()
		{
			if (_cs.IsCompleted)
				throw new InvalidOperationException("Asynchronous enumeration is already completed.");
		}

		/// <summary>
		/// Adds a new value to the enumeration.
		/// </summary>
		/// <param name="value">The value to add.</param>
		/// <exception cref="InvalidOperationException">Thrown if enumeration is finished.</exception>
		protected void Add(T value)
		{
			lock (_lock)
			{
				CheckFinish();
				_values.Add(value);
				_signal.Pulse();
			}
		}

		/// <summary>
		/// Adds a new values to the enumeration.
		/// </summary>
		/// <param name="values">The values to add.</param>
		/// <exception cref="InvalidOperationException">Thrown if enumeration is finished.</exception>
		protected void AddRange(IEnumerable<T> values)
		{
			lock (_lock)
			{
				CheckFinish();
				_values.AddRange(values);
				_signal.Pulse();
			}
		}

		/// <summary>
		/// Marks the enumeration as succeed complete without adding a final value.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if enumeration is already finished.</exception>
		protected void Finish()
		{
			lock (_lock)
			{
				CheckFinish();
				_cs.Complete();
				_signal.Pulse();
			}
		}

		/// <summary>
		/// Marks the enumeration as succeed complete and adds a final value.
		/// </summary>
		/// <param name="value">The final value to add.</param>
		/// <exception cref="InvalidOperationException">Thrown if enumeration is already finished.</exception>
		protected void Finish(T value)
		{
			lock (_lock)
			{
				CheckFinish();
				_values.Add(value);
				_cs.Complete();
				_signal.Pulse();
			}
		}

		/// <summary>
		/// Completes the enumeration as cancelled.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if enumeration is already finished.</exception>
		protected void Cancel()
		{
			lock (_lock)
			{
				CheckFinish();
				_cs.Cancel();
				_signal.Pulse();
			}
		}

		/// <summary>
		/// Completes the enumeration as faulted.
		/// </summary>
		/// <param name="exception">The exception that caused the failure.</param>
		/// <exception cref="InvalidOperationException">Thrown if enumeration is already finished.</exception>
		protected void Fail(Exception exception)
		{
			lock (_lock)
			{
				CheckFinish();
				_cs.Fail(exception);
				_signal.Pulse();
			}
		}

		/// <summary>
		/// Imports the completion from the <see cref="CompletedEventArgs"/>.
		/// </summary>
		/// <param name="args">The completed event args to import from.</param>
		protected void ImportCompletion(CompletedEventArgs args)
		{
			lock (_lock)
			{
				CheckFinish();
				_cs.Import(args);
				_signal.Pulse();
			}
		}
	}
}