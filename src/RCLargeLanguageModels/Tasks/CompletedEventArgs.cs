using System;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Tasks
{
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
}