using System;
using System.Threading;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Tasks
{
	// Как же я люблю писать код и закидывать его в нейронку на доработку ツ

	/// <summary>
	/// Provides an asynchronous debouncing mechanism to delay execution until a specified time has passed without additional calls.
	/// </summary>
	/// <remarks>
	/// This class is thread-safe and ensures only the last call within the debounce period executes its action.
	/// Use it to throttle rapid invocations, such as UI events or API calls.
	/// </remarks>
	public class Debounce : IDisposable
	{
		private CancellationTokenSource _cts = new CancellationTokenSource();
		private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
		private readonly bool _debouncePassedResult;
		private bool _isDisposed;

		/// <summary>
		/// Gets the cancellation token that signals cancellation after the debounce period or when a new call occurs.
		/// </summary>
		/// <remarks>
		/// Use that token to cancel the async actions in the debounced functions, like this:
		/// <code>
		/// var debounce = new Debounce();
		/// 
		/// // ... The debounce code ...
		/// 
		/// // The operation will be cancelled when the debounce function is called again
		/// await SomeAsyncOperation(debounce.CancellationToken);
		/// </code>
		/// </remarks>
		public CancellationToken CancellationToken => _cts.Token;

		/// <summary>
		/// Gets the result returned when the debounce period completes successfully.
		/// </summary>
		public bool SuccessResult => _debouncePassedResult;

		/// <summary>
		/// Initializes a new instance of the <see cref="Debounce"/> class.
		/// </summary>
		/// <param name="debouncePassedResult">The value returned if the debounce period completes without interruption.
		/// Defaults to <see langword="false"/> for suitable use with <see langword="if"/> operator.</param>
		public Debounce(bool debouncePassedResult = false)
		{
			_debouncePassedResult = debouncePassedResult;
		}

		/// <summary>
		/// Delays execution until the specified time has passed without additional calls, supporting external cancellation.
		/// </summary>
		/// <param name="delay">The time to wait before considering the debounce successful.</param>
		/// <param name="externalCancellationToken">An optional token to cancel the debounce externally. Defaults to <see cref="CancellationToken.None"/>.</param>
		/// <returns>
		/// A <see cref="Task"/> that returns <see cref="SuccessResult"/> if the debounce period completes,
		/// or the opposite value if canceled (either by a new call or externally).
		/// </returns>
		/// <remarks>
		/// Use this method to debounce an action, like this:
		/// <code>
		/// // Setup the debounce to return false if it passes
		/// var debounce = new Debounce(debouncePassedResult: false);
		/// 
		/// ...
		/// 
		/// // ... The method that need to be debounced ...
		/// if (await debounce.DebounceAsync(TimeSpan.FromMilliseconds(500)))
		///     return; // Debounce passed, no action needed yet
		/// 
		/// Console.WriteLine("Action executed after debounce");
		/// 
		/// // Pass the debounce cancellation token to the async operation
		/// await SomeAsyncOperation(debounce.CancellationToken);
		/// </code>
		/// </remarks>
		/// <exception cref="ObjectDisposedException">Thrown if the instance has been disposed.</exception>
		public async Task<bool> DebounceAsync(TimeSpan delay, CancellationToken externalCancellationToken = default)
		{
			if (_isDisposed)
				throw new ObjectDisposedException(nameof(Debounce));

			CancellationTokenSource oldCts = null;
			try
			{
				await _semaphore.WaitAsync(externalCancellationToken).ConfigureAwait(false);
				
				oldCts = _cts;
				_cts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken);
				oldCts.Cancel();
			}
			catch (OperationCanceledException)
			{
				return !_debouncePassedResult;
			}
			finally
			{
				_semaphore.Release();
				oldCts?.Dispose();
			}

			try
			{
				await Task.Delay(delay, _cts.Token).ConfigureAwait(false);
				return _debouncePassedResult;
			}
			catch (TaskCanceledException)
			{
				return !_debouncePassedResult;
			}
		}

		/// <summary>
		/// Delays execution until the specified time in milliseconds has passed without additional calls.
		/// </summary>
		/// <param name="millisecondsDelay">The delay in milliseconds before considering the debounce successful.</param>
		/// <param name="externalCancellationToken">An optional token to cancel the debounce externally. Defaults to <see cref="CancellationToken.None"/>.</param>
		/// <returns>
		/// A <see cref="Task"/> that returns <see cref="SuccessResult"/> if the debounce period completes,
		/// or the opposite value if canceled (either by a new call or externally).
		/// </returns>
		/// <remarks>
		/// Use this method to debounce an action, like this:
		/// <code>
		/// // Setup the debounce to return false
		/// var debounce = new Debounce(debouncePassedResult: false);
		/// 
		/// ...
		/// 
		/// // *The method that need to be debounced*
		/// if (await debounce.DebounceAsync(millisecondsDelay: 500))
		///     return; // Debounce passed, no action needed yet
		/// 
		/// Console.WriteLine("Action executed after debounce");
		/// 
		/// // Pass the debounce cancellation token to the async operation
		/// await SomeAsyncOperation(debounce.CancellationToken);
		/// </code>
		/// </remarks>
		/// <exception cref="ObjectDisposedException">Thrown if the instance has been disposed.</exception>
		public Task<bool> DebounceAsync(int millisecondsDelay, CancellationToken externalCancellationToken = default)
		{
			return DebounceAsync(TimeSpan.FromMilliseconds(millisecondsDelay), externalCancellationToken);
		}

		~Debounce()
		{
			Dispose(false);
		}

		private void Dispose(bool disposeManaged)
		{
			if (_isDisposed)
				return;

			if (disposeManaged)
			{
				_cts.Cancel();
				_cts.Dispose();
				_semaphore.Dispose();
			}

			_isDisposed = true;
		}

		/// <summary>
		/// Releases all resources used by the <see cref="Debounce"/> instance.
		/// </summary>
		public void Dispose()
		{
			GC.SuppressFinalize(this);
			Dispose(true);
		}
	}
}