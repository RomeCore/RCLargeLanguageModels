using System;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Tasks
{
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
		/// The duck-typed method that makes this class considered as awaiter.
		/// </summary>
		public void GetResult()
		{
			switch (State)
			{
				case CompletionState.Incomplete:
				case CompletionState.Success:
					break;

				case CompletionState.Cancelled:
					throw new OperationCanceledException("Operation was canceled.");

				case CompletionState.Failed:
					throw new AggregateException("One or more errors occurred.", Exception);
			}
		}

		public static CompletionToken Success { get; } = new CompletionToken(CompletionState.Success);
		public static CompletionToken Cancelled { get; } = new CompletionToken(CompletionState.Cancelled);
		public static CompletionToken Failed(Exception exception) => new CompletionToken(CompletionState.Failed, exception);
	}
}