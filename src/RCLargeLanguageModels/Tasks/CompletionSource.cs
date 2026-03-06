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
	/// Represents a completion state.
	/// </summary>
	public enum CompletionState
	{
		/// <summary>
		/// The incomplete state.
		/// </summary>
		Incomplete,

		/// <summary>
		/// The success state.
		/// </summary>
		Success,

		/// <summary>
		/// The cancelled state.
		/// </summary>
		Cancelled,

		/// <summary>
		/// The failed state.
		/// </summary>
		Failed
	}

	/// <summary>
	/// The class that contains the arguments of the 'Completed' events.
	/// </summary>
	public class CompletedEventArgs
	{
		/// <summary>
		/// The new state of the completion, always not equal to <see cref="CompletionState.Incomplete"/>.
		/// </summary>
		public CompletionState State { get; }

		/// <summary>
		/// The exception that caused the completion state to be considered failed.
		/// Always not <see langword="null"/> when <see cref="CompletionState"/> is <see cref="CompletionState.Failed"/>.
		/// </summary>
		public Exception Exception { get; }

		/// <summary>
		/// Creates a new instance of <see cref="CompletedEventArgs"/>.
		/// </summary>
		/// <param name="state">The new state of completion. Must be not <see cref="CompletionState.Incomplete"/>.</param>
		/// <param name="exception">The exception for info that must present along with <see cref="CompletionState.Failed"/>.</param>
		public CompletedEventArgs(CompletionState state, Exception exception)
		{
			if (state == CompletionState.Incomplete)
				throw new ArgumentException("The state must be not equal to CompletionState.Incomplete.", nameof(state));
			if (state == CompletionState.Failed && exception == null)
				throw new ArgumentNullException(nameof(exception));

			State = state;
			Exception = state == CompletionState.Failed ? exception : null;
		}

		/// <summary>
		/// Transfers the completion state to a <see cref="TaskCompletionSource{TResult}"/> with a default success value.
		/// </summary>
		/// <typeparam name="T">The type of the result produced by the task.</typeparam>
		/// <param name="tcs">The target TaskCompletionSource to set the completion state on.</param>
		/// <remarks>
		/// For successful completion, the task will be completed with a default value of type T.
		/// </remarks>
		public void ExportToTcs<T>(TaskCompletionSource<T> tcs)
		{
			ExportToTcs(tcs, () => default);
		}

		/// <summary>
		/// Transfers the completion state to a <see cref="TaskCompletionSource{TResult}"/> with a specified success value.
		/// </summary>
		/// <typeparam name="T">The type of the result produced by the task.</typeparam>
		/// <param name="tcs">The target TaskCompletionSource to set the completion state on.</param>
		/// <param name="successValue">The value to set as the result when the completion state is successful.</param>
		public void ExportToTcs<T>(TaskCompletionSource<T> tcs, T successValue)
		{
			ExportToTcs(tcs, () => successValue);
		}

		/// <summary>
		/// Transfers the completion state to a <see cref="TaskCompletionSource{TResult}"/> using a factory function for the success value.
		/// </summary>
		/// <typeparam name="T">The type of the result produced by the task.</typeparam>
		/// <param name="tcs">The target <see cref="TaskCompletionSource{TResult}"/> to set the completion state on.</param>
		/// <param name="successFactory">The factory function that produces the result value when the completion state is successful.</param>
		/// <remarks>
		/// The factory function will only be invoked if the completion state is successful.
		/// </remarks>
		public void ExportToTcs<T>(TaskCompletionSource<T> tcs, Func<T> successFactory)
		{
			switch (State)
			{
				case CompletionState.Success:
					tcs.SetResult(successFactory());
					break;
				case CompletionState.Cancelled:
					tcs.SetCanceled();
					break;
				case CompletionState.Failed:
					tcs.SetException(Exception);
					break;
			}
		}
	}

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

	/// <summary>
	/// Represents a token of read-only completion state.
	/// </summary>
	public class CompletionToken : INotifyCompletion
	{
		private CompletionSource _source;

		/// <summary>
		/// Gets the state of the completion.
		/// </summary>
		public CompletionState State => _source.State;

		/// <summary>
		/// Gets a value indicating whether the completion state is considered completed.
		/// </summary>
		public bool IsCompleted => _source.IsCompleted;

		/// <summary>
		/// Gets the exception that caused the completion state to be considered failed. May be <see langword="null"/>.
		/// </summary>
		public Exception Exception => _source.Exception;

		/// <summary>
		/// Gets the task representation of the completion state.
		/// </summary>
		public Task Task => _source.Task;
		
		/// <summary>
		/// Gets the safe task representation of the completion state.
		/// </summary>
		/// <remarks>
		/// The task is guaranteed to not be failed or cancelled.
		/// </remarks>
		public Task SafeTask => _source.SafeTask;

		/// <summary>
		/// The event that is raised when the completion state is considered completed.
		/// </summary>
		public event EventHandler<CompletedEventArgs>? Completed;

		private void SetSource(CompletionSource source)
		{
			_source = source;
			source.Completed += (s, e) => Completed?.Invoke(this, e);
		}

		/// <summary>
		/// Creates a new empty instance of <see cref="CompletionToken"/>.
		/// </summary>
		public CompletionToken()
		{
			SetSource(new CompletionSource());
		}

		/// <summary>
		/// Creates a new instance of <see cref="CompletionToken"/>.
		/// </summary>
		/// <param name="state">The initial state for completion.</param>
		/// <param name="exception">The exception that contains info about </param>
		[JsonConstructor]
		public CompletionToken(CompletionState state, Exception exception = null)
		{
			SetSource(new CompletionSource(state, exception));
		}

		/// <summary>
		/// Creates a new instance of <see cref="CompletionToken"/>.
		/// </summary>
		/// <param name="source">The completion source used to read the completion state.</param>
		public CompletionToken(CompletionSource source)
		{
			SetSource(source);
		}

		/// <summary>
		/// Creates the new instance of <see cref="CompletionToken"/> along with source to used to control the token.
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public static CompletionToken Create(out CompletionSource source)
		{
			source = new CompletionSource();
			return new CompletionToken(source);
		}

		/// <summary>
		/// Throws exception if the completion token is already completed.
		/// </summary>
		/// <param name="message">The optional message to show in exception.</param>
		/// <exception cref="InvalidOperationException"></exception>
		public void ThrowIfComplete(string message = null)
		{
			_source.ThrowIfComplete(message);
		}

		/// <summary>
		/// Calls the specified handler when the completion state is considered completed.
		/// May call the handler instantly if the completion state is already completed.
		/// </summary>
		/// <param name="handler">The event handler to call when the completion state is considered completed.</param>
		public void OnCompleted(EventHandler<CompletedEventArgs> handler)
		{
			_source.OnCompleted((s, e) =>
			{
				handler(this, e);
			});
		}

		/// <summary>
		/// Calls the specified handler when the completion state is considered completed.
		/// May call the handler instantly if the completion state is already completed.
		/// </summary>
		/// <param name="continuation">The handler that calls when this info is completed.</param>
		public void OnCompleted(Action continuation)
		{
			_source.OnCompleted((s, e) =>
			{
				continuation?.Invoke();
			});
		}

		/// <summary>
		/// The duck-typed blank method that makes this class considered as awaiter.
		/// </summary>
		public void GetResult()
		{
		}

		public static CompletionToken Success { get; } = new CompletionToken(CompletionState.Success);
		public static CompletionToken Cancelled { get; } = new CompletionToken(CompletionState.Cancelled);
		public static CompletionToken Failed(Exception exception) => new CompletionToken(CompletionState.Failed, exception);
	}
}