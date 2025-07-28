using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Tasks
{
	/// <summary>
	/// Represents a pulsing async signal with synchronous/asynchronous waiting methods.
	/// </summary>
	public class AsyncSignal
	{
		private readonly object _lockObj = new object();
		private TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();

		/// <summary>
		/// Synchronously waits pulse.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token used to cancel the waiting.</param>
		public void Wait(CancellationToken cancellationToken = default)
		{
			TaskCompletionSource<object> tcs;
			lock (_lockObj)
			{
				tcs = _tcs;
			}
			tcs.Task.Wait(cancellationToken);
		}

		/// <summary>
		/// Asynchronously waits pulse.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token used to cancel the waiting.</param>
		/// <returns>The task represents an asynchronous wait operation.</returns>
		/// <exception cref="TaskCanceledException">Thrown when cancellation is triggered.</exception>
		public async Task WaitAsync(CancellationToken cancellationToken = default)
		{
			if (cancellationToken.IsCancellationRequested)
				throw new TaskCanceledException();

			TaskCompletionSource<object> tcs;
			lock (_lockObj)
			{
				tcs = _tcs;
			}

			var delayTask = Task.Delay(Timeout.Infinite, cancellationToken);
			var completedTask = await Task.WhenAny(tcs.Task, delayTask);

			if (completedTask == delayTask)
				throw new TaskCanceledException();
		}

		/// <summary>
		/// Pulses the signal and causes all awaiters to make through.
		/// </summary>
		public void Pulse()
		{
			lock (_lockObj)
			{
				var tcs = _tcs;
				_tcs = new TaskCompletionSource<object>();
				tcs.SetResult(null);
			}
		}
	}
}