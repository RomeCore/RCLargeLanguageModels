using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Tasks
{
	/// <summary>
	/// Represents a completion source used to control the <see cref="CompletionToken"/>.
	/// </summary>
	public class CompletionSource
	{
		private readonly object _syncLock = new object();
		private readonly TaskCompletionSource<object> _tcs, _stcs;
		private CompletionState _state;
		private Exception _exception;

		/// <summary>
		/// Gets the <see cref="CompletionToken"/> associated with this source.
		/// </summary>
		public CompletionToken Token { get; }

		/// <summary>
		/// Gets the state of the completion source.
		/// </summary>
		public CompletionState State { get
			{
				lock (_syncLock)
					return _state;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the completion state is considered completed.
		/// </summary>
		public bool IsCompleted => State != CompletionState.Incomplete;

		/// <summary>
		/// Gets the exception that caused the completion state to be considered failed. May be <see langword="null"/>.
		/// </summary>
		public Exception Exception
		{
			get
			{
				lock (_syncLock)
					return _exception;
			}
		}

		/// <summary>
		/// Gets the task representation of the completion state.
		/// </summary>
		public Task Task
		{
			get
			{
				lock (_syncLock)
					return _tcs.Task;
			}
		}

		/// <summary>
		/// Gets the safe task representation of the completion state.
		/// </summary>
		/// <remarks>
		/// The task is guaranteed to not be failed or cancelled.
		/// </remarks>
		public Task SafeTask
		{
			get
			{
				lock (_syncLock)
					return _stcs.Task;
			}
		}

		/// <summary>
		/// The event that is raised when the completion state is considered completed.
		/// </summary>
		public event EventHandler<CompletedEventArgs> Completed;

		/// <summary>
		/// Creates a new empty instance of <see cref="CompletionSource"/> with <see cref="CompletionState.Incomplete"/> state.
		/// </summary>
		public CompletionSource()
		{
			Token = new CompletionToken(this);
			_tcs = new TaskCompletionSource<object>();
			_stcs = new TaskCompletionSource<object>();
			_state = CompletionState.Incomplete;
		}

		/// <summary>
		/// Creates a new instance of <see cref="CompletionSource"/>.
		/// </summary>
		/// <param name="state">The inital state of the completion token, must be not <see cref="CompletionState.Failed"/>.</param>
		public CompletionSource(CompletionState state)
		{
			Token = new CompletionToken(this);
			_tcs = new TaskCompletionSource<object>();
			_stcs = new TaskCompletionSource<object>();
			_state = state;

			switch (state)
			{
				case CompletionState.Incomplete:
					break;

				case CompletionState.Success:
					_tcs.SetResult(null);
					_stcs.SetResult(null);
					break;

				case CompletionState.Cancelled:
					_tcs.SetCanceled();
					_stcs.SetResult(null);
					break;

				case CompletionState.Failed:
					throw new ArgumentException($"State is {nameof(CompletionState.Failed)} but no exception is provided!", nameof(state));

				default:
					throw new ArgumentOutOfRangeException(nameof(state), "Unknown completion state!");
			}
		}

		/// <summary>
		/// Creates a new instance of <see cref="CompletionSource"/>.
		/// </summary>
		/// <param name="state">The inital state of the completion source.</param>
		/// <param name="exception">The exception of the 'failed' state.
		/// Must be non-<see langword="null"/> when <paramref name="state"/> is <see cref="CompletionState.Failed"/>.</param>
		public CompletionSource(CompletionState state, Exception exception)
		{
			Token = new CompletionToken(this);
			_tcs = new TaskCompletionSource<object>();
			_stcs = new TaskCompletionSource<object>();
			_state = state;

			switch (state)
			{
				case CompletionState.Incomplete:
					break;

				case CompletionState.Success:
					_tcs.SetResult(null);
					_stcs.SetResult(null);
					break;

				case CompletionState.Cancelled:
					_tcs.SetCanceled();
					_stcs.SetResult(null);
					break;

				case CompletionState.Failed:
					if (exception == null)
						throw new ArgumentNullException(nameof(exception));
					_exception = exception;
					_tcs.SetException(exception);
					_stcs.SetResult(null);
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(state), "Unknown completion state!");
			}
		}

		/// <summary>
		/// Successfully completes the source.
		/// </summary>
		public void Complete()
		{
			lock (_syncLock)
			{
				if (_state != CompletionState.Incomplete)
					return;

				_state = CompletionState.Success;
				_tcs.SetResult(null);
				_stcs.SetResult(null);
				Completed?.Invoke(this, new CompletedEventArgs(CompletionState.Success, null));
			}
		}

		/// <summary>
		/// Completes the source with cancellation.
		/// </summary>
		public void Cancel()
		{
			lock (_syncLock)
			{
				if (_state != CompletionState.Incomplete)
					return;

				_state = CompletionState.Cancelled;
				_tcs.SetCanceled();
				_stcs.SetResult(null);
				Completed?.Invoke(this, new CompletedEventArgs(CompletionState.Cancelled, null));
			}
		}

		/// <summary>
		/// Completes the source with exception.
		/// </summary>
		/// <param name="exception">The exception to complete with. Must be non-<see langword="null"/>.</param>
		/// <exception cref="ArgumentNullException"></exception>
		public void Fail(Exception exception)
		{
			if (exception == null)
				throw new ArgumentNullException(nameof(exception));

			lock (_syncLock)
			{
				if (_state != CompletionState.Incomplete)
					return;

				_state = CompletionState.Failed;
				_exception = exception;
				_tcs.SetException(exception);
				_stcs.SetResult(null);
				Completed?.Invoke(this, new CompletedEventArgs(CompletionState.Failed, exception));
			}
		}

		/// <summary>
		/// Throws exception if the completion source is already completed.
		/// </summary>
		/// <param name="message">The optional message to show in exception.</param>
		/// <exception cref="InvalidOperationException"></exception>
		public void ThrowIfComplete(string message = null)
		{
			lock (_syncLock)
				if (_state == CompletionState.Incomplete)
					return;

			if (string.IsNullOrEmpty(message))
				throw new InvalidOperationException("The completion token is already completed.");
			else
				throw new InvalidOperationException(message);
		}

		/// <summary>
		/// Calls the specified handler when the completion state is considered completed.
		/// May call the handler instantly if the completion state is already completed.
		/// </summary>
		/// <param name="handler">The event handler to call when the completion state is considered completed.</param>
		public void OnCompleted(EventHandler<CompletedEventArgs> handler)
		{
			lock (_syncLock)
			{
				if (_state != CompletionState.Incomplete)
				{
					handler(this, new CompletedEventArgs(_state, _exception));
					return;
				}
			}
			Completed += handler;
		}

		/// <summary>
		/// Imports the completion state from <see cref="CompletedEventArgs"/>.
		/// </summary>
		/// <param name="e">The <see cref="CompletedEventArgs"/> to import from.</param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		public void Import(CompletedEventArgs e)
		{
			if (e == null)
				throw new ArgumentNullException(nameof(e));
			if (_state != CompletionState.Incomplete)
				throw new InvalidOperationException("The completion token is already completed.");

			switch (e.State)
			{
				case CompletionState.Success:
					Complete();
					break;

				case CompletionState.Cancelled:
					Cancel();
					break;

				case CompletionState.Failed:
					Fail(e.Exception);
					break;
			}
		}

		public static CompletionSource Success { get; } = new CompletionSource(CompletionState.Success);
		public static CompletionSource Cancelled { get; } = new CompletionSource(CompletionState.Cancelled);
		public static CompletionSource Failed(Exception exception) => new CompletionSource(CompletionState.Failed, exception);
	}
}