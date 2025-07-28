using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Tasks
{
	/// <summary>
	/// The task queue unit
	/// </summary>
	public class TaskUnit
	{
		/// <summary>
		/// The task factory function used to create the task
		/// </summary>
		public Func<Task> TaskFactory { get; }

		/// <summary>
		/// The cancellation token used to cancel the task and to prevent execution inside queue
		/// </summary>
		public CancellationToken CancellationToken { get; }

		/// <summary>
		/// The callback function called when the task is cancelled from queue execution <br/>
		/// Can be <see langword="null"/>
		/// </summary>
		/// <remarks>
		/// This is called inside queue
		/// </remarks>
		public Action OnCancel { get; }

		private TaskUnit(Func<Task> taskFactory, CancellationToken cancellationToken, Action onCancel)
		{
			TaskFactory = taskFactory;
			CancellationToken = cancellationToken;
			OnCancel = onCancel;
		}

		private static (Action completeCallback, Action cancelCallback, Action<Exception> failCallback)
			WrapCallbacks(Action completeCallback, Action cancelCallback, Action<Exception> failCallback, SynchronizationContext syncContext)
		{
			if (syncContext == null)
				return (completeCallback, cancelCallback, failCallback);

			Action wrappedCompleteCallback = completeCallback;
			if (completeCallback != null)
				wrappedCompleteCallback = () => syncContext.Post(state => completeCallback(), null);

			Action wrappedCancelCallback = cancelCallback;
			if (cancelCallback != null)
				wrappedCancelCallback = () => syncContext.Post(state => cancelCallback(), null);

			Action<Exception> wrappedFailCallback = failCallback;
			if (failCallback != null)
				wrappedFailCallback = e => syncContext.Post(state => failCallback(e), null);

			return (wrappedCompleteCallback, wrappedCancelCallback, wrappedFailCallback);
		}

		private static (Action<T> completeCallback, Action cancelCallback, Action<Exception> failCallback)
			WrapCallbacks<T>(Action<T> completeCallback, Action cancelCallback, Action<Exception> failCallback, SynchronizationContext syncContext)
		{
			if (syncContext == null)
				return (completeCallback, cancelCallback, failCallback);

			Action<T> wrappedCompleteCallback = completeCallback;
			if (completeCallback != null)
				wrappedCompleteCallback = x => syncContext.Post(state => completeCallback(x), null);

			Action wrappedCancelCallback = cancelCallback;
			if (cancelCallback != null)
				wrappedCancelCallback = () => syncContext.Post(state => cancelCallback(), null);

			Action<Exception> wrappedFailCallback = failCallback;
			if (failCallback != null)
				wrappedFailCallback = e => syncContext.Post(state => failCallback(e), null);

			return (wrappedCompleteCallback, wrappedCancelCallback, wrappedFailCallback);
		}

		/// <summary>
		/// Creates a <see cref="TaskUnit"/> for a non-generic asynchronous operation with optional completion, cancellation, and failure callbacks
		/// </summary>
		/// <param name="asyncFunction">The asynchronous operation to execute</param>
		/// <param name="completeCallback">The callback invoked upon successful completion, or <see langword="null"/> if none</param>
		/// <param name="cancelCallback">The callback invoked if the task is canceled, or <see langword="null"/> if none</param>
		/// <param name="failCallback">The callback invoked if an exception occurs, or <see langword="null"/> if none</param>
		/// <param name="syncContext">The syncronization context used to wrap callbacks, or <see langword="null"/> if none</param>
		/// <param name="cancellationToken">The token used to cancel the task. Defaults to <see cref="CancellationToken.None"/></param>
		/// <returns>A new <see cref="TaskUnit"/> instance configured with the specified operation and callbacks</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncFunction"/> is <see langword="null"/></exception>
		public static TaskUnit Create(Func<Task> asyncFunction, Action completeCallback = null, Action cancelCallback = null, Action<Exception> failCallback = null, SynchronizationContext syncContext = null, CancellationToken cancellationToken = default)
		{
			if (asyncFunction == null)
				throw new ArgumentNullException(nameof(asyncFunction));

			(completeCallback, cancelCallback, failCallback) = WrapCallbacks(completeCallback, cancelCallback, failCallback, syncContext);

			return new TaskUnit(async () =>
			{
				try
				{
					await asyncFunction();
					completeCallback?.Invoke();
				}
				catch (TaskCanceledException)
				{
					cancelCallback?.Invoke();
				}
				catch (OperationCanceledException)
				{
					cancelCallback?.Invoke();
				}
				catch (Exception e)
				{
					failCallback?.Invoke(e);
				}
			}, cancellationToken, cancelCallback);
		}

		/// <summary>
		/// Creates a <see cref="TaskUnit"/> for a generic asynchronous operation with optional completion, cancellation, and failure callbacks
		/// </summary>
		/// <typeparam name="T">The type of the result returned by the asynchronous operation</typeparam>
		/// <param name="asyncFunction">The asynchronous operation that returns a value of type <typeparamref name="T"/></param>
		/// <param name="completeCallback">The callback invoked with the result upon successful completion, or <see langword="null"/> if none</param>
		/// <param name="cancelCallback">The callback invoked if the task is canceled, or <see langword="null"/> if none</param>
		/// <param name="failCallback">The callback invoked if an exception occurs, or <see langword="null"/> if none</param>
		/// <param name="syncContext">The syncronization context used to wrap callbacks, or <see langword="null"/> if none</param>
		/// <param name="cancellationToken">The token used to cancel the task. Defaults to <see cref="CancellationToken.None"/></param>
		/// <returns>A new <see cref="TaskUnit"/> instance configured with the specified operation and callbacks</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncFunction"/> is <see langword="null"/></exception>
		public static TaskUnit Create<T>(Func<Task<T>> asyncFunction, Action<T> completeCallback = null, Action cancelCallback = null, Action<Exception> failCallback = null, SynchronizationContext syncContext = null, CancellationToken cancellationToken = default)
		{
			if (asyncFunction == null)
				throw new ArgumentNullException(nameof(asyncFunction));

			(completeCallback, cancelCallback, failCallback) = WrapCallbacks(completeCallback, cancelCallback, failCallback, syncContext);

			return new TaskUnit(async () =>
			{
				try
				{
					var result = await asyncFunction();
					completeCallback?.Invoke(result);
				}
				catch (TaskCanceledException)
				{
					cancelCallback?.Invoke();
				}
				catch (OperationCanceledException)
				{
					cancelCallback?.Invoke();
				}
				catch (Exception e)
				{
					failCallback?.Invoke(e);
				}
			}, cancellationToken, cancelCallback);
		}

		/// <summary>
		/// Creates a <see cref="TaskUnit"/> for a non-generic asynchronous operation and provides a <see cref="Task"/> for external tracking
		/// </summary>
		/// <param name="asyncFunction">The asynchronous operation to execute</param>
		/// <param name="task">When this method returns, contains the <see cref="Task"/> representing the operation's completion state</param>
		/// <param name="cancellationToken">The token used to cancel the task. Defaults to <see cref="CancellationToken.None"/></param>
		/// <returns>A new <see cref="TaskUnit"/> instance configured with the specified operation</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncFunction"/> is <see langword="null"/></exception>
		public static TaskUnit Create(Func<Task> asyncFunction, out Task task, CancellationToken cancellationToken = default)
		{
			if (asyncFunction == null)
				throw new ArgumentNullException(nameof(asyncFunction));

			var taskSource = new TaskCompletionSource<object>();
			task = taskSource.Task;
			return Create(asyncFunction, taskSource, cancellationToken);
		}

		/// <summary>
		/// Creates a <see cref="TaskUnit"/> for a non-generic asynchronous operation using a specified <see cref="TaskCompletionSource{Object}"/>
		/// </summary>
		/// <param name="asyncFunction">The asynchronous operation to execute</param>
		/// <param name="taskSource">The <see cref="TaskCompletionSource{Object}"/> used to control the task's completion state</param>
		/// <param name="cancellationToken">The token used to cancel the task. Defaults to <see cref="CancellationToken.None"/></param>
		/// <returns>A new <see cref="TaskUnit"/> instance configured with the specified operation and task source</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncFunction"/> or <paramref name="taskSource"/> is <see langword="null"/></exception>
		public static TaskUnit Create(Func<Task> asyncFunction, TaskCompletionSource<object> taskSource, CancellationToken cancellationToken = default)
		{
			if (asyncFunction == null)
				throw new ArgumentNullException(nameof(asyncFunction));
			if (taskSource == null)
				throw new ArgumentNullException(nameof(taskSource));
			
			return new TaskUnit(async () =>
			{
				try
				{
					await asyncFunction();
					taskSource.SetResult(null);
				}
				catch (TaskCanceledException)
				{
					taskSource.SetCanceled();
				}
				catch (OperationCanceledException)
				{
					taskSource.SetCanceled();
				}
				catch (Exception e)
				{
					taskSource.SetException(e);
				}
			}, cancellationToken, () => taskSource.SetCanceled());
		}

		/// <summary>
		/// Creates a <see cref="TaskUnit"/> for a generic asynchronous operation and provides a <see cref="Task{T}"/> for external tracking
		/// </summary>
		/// <typeparam name="T">The type of the result returned by the asynchronous operation</typeparam>
		/// <param name="asyncFunction">The asynchronous operation that returns a value of type <typeparamref name="T"/></param>
		/// <param name="task">When this method returns, contains the <see cref="Task{T}"/> representing the operation's completion state</param>
		/// <param name="cancellationToken">The token used to cancel the task. Defaults to <see cref="CancellationToken.None"/></param>
		/// <returns>A new <see cref="TaskUnit"/> instance configured with the specified operation</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncFunction"/> is <see langword="null"/></exception>
		public static TaskUnit Create<T>(Func<Task<T>> asyncFunction, out Task<T> task, CancellationToken cancellationToken = default)
		{
			if (asyncFunction == null)
				throw new ArgumentNullException(nameof(asyncFunction));
			
			var taskSource = new TaskCompletionSource<T>();
			task = taskSource.Task;
			return Create(asyncFunction, taskSource, cancellationToken);
		}

		/// <summary>
		/// Creates a <see cref="TaskUnit"/> for a generic asynchronous operation using a specified <see cref="TaskCompletionSource{T}"/>
		/// </summary>
		/// <typeparam name="T">The type of the result returned by the asynchronous operation</typeparam>
		/// <param name="asyncFunction">The asynchronous operation that returns a value of type <typeparamref name="T"/></param>
		/// <param name="taskSource">The <see cref="TaskCompletionSource{T}"/> used to control the task's completion state</param>
		/// <param name="cancellationToken">The token used to cancel the task. Defaults to <see cref="CancellationToken.None"/></param>
		/// <returns>A new <see cref="TaskUnit"/> instance configured with the specified operation and task source</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncFunction"/> or <paramref name="taskSource"/> is <see langword="null"/></exception>
		public static TaskUnit Create<T>(Func<Task<T>> asyncFunction, TaskCompletionSource<T> taskSource, CancellationToken cancellationToken = default)
		{
			if (asyncFunction == null)
				throw new ArgumentNullException(nameof(asyncFunction));
			if (taskSource == null)
				throw new ArgumentNullException(nameof(taskSource));

			return new TaskUnit(async () =>
			{
				try
				{
					var result = await asyncFunction();
					taskSource.SetResult(result);
				}
				catch (TaskCanceledException)
				{
					taskSource.SetCanceled();
				}
				catch (OperationCanceledException)
				{
					taskSource.SetCanceled();
				}
				catch (Exception e)
				{
					taskSource.SetException(e);
				}
			}, cancellationToken, () => taskSource.SetCanceled());
		}

		/// <summary>
		/// Creates a <see cref="TaskUnit"/> for a non-generic asynchronous operation with callbacks and provides a <see cref="Task"/> for external tracking.
		/// </summary>
		/// <param name="asyncFunction">The asynchronous operation to execute.</param>
		/// <param name="completeCallback">The callback invoked upon successful completion, or <see langword="null"/> if none.</param>
		/// <param name="cancelCallback">The callback invoked if the task is canceled, or <see langword="null"/> if none.</param>
		/// <param name="failCallback">The callback invoked if an exception occurs, or <see langword="null"/> if none.</param>
		/// <param name="task">When this method returns, contains the <see cref="Task"/> representing the operation's completion state.</param>
		/// <param name="syncContext">The synchronization context used to wrap callbacks, or <see langword="null"/> if none.</param>
		/// <param name="cancellationToken">The token used to cancel the task. Defaults to <see cref="CancellationToken.None"/>.</param>
		/// <returns>A new <see cref="TaskUnit"/> instance configured with the specified operation, callbacks, and task tracking.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncFunction"/> is <see langword="null"/>.</exception>
		public static TaskUnit Create(Func<Task> asyncFunction, Action completeCallback, Action cancelCallback, Action<Exception> failCallback,
			out Task task, SynchronizationContext syncContext = null, CancellationToken cancellationToken = default)
		{
			if (asyncFunction == null)
				throw new ArgumentNullException(nameof(asyncFunction));

			var taskSource = new TaskCompletionSource<object>();
			task = taskSource.Task;

			(completeCallback, cancelCallback, failCallback) = WrapCallbacks(completeCallback, cancelCallback, failCallback, syncContext);

			return new TaskUnit(async () =>
			{
				try
				{
					await asyncFunction();
					completeCallback?.Invoke();
					taskSource.SetResult(null);
				}
				catch (TaskCanceledException)
				{
					cancelCallback?.Invoke();
					taskSource.SetCanceled();
				}
				catch (OperationCanceledException)
				{
					cancelCallback?.Invoke();
					taskSource.SetCanceled();
				}
				catch (Exception e)
				{
					failCallback?.Invoke(e);
					taskSource.SetException(e);
				}
			}, cancellationToken, cancelCallback);
		}

		/// <summary>
		/// Creates a <see cref="TaskUnit"/> for a non-generic asynchronous operation with callbacks and a specified <see cref="TaskCompletionSource{Object}"/>.
		/// </summary>
		/// <param name="asyncFunction">The asynchronous operation to execute.</param>
		/// <param name="completeCallback">The callback invoked upon successful completion, or <see langword="null"/> if none.</param>
		/// <param name="cancelCallback">The callback invoked if the task is canceled, or <see langword="null"/> if none.</param>
		/// <param name="failCallback">The callback invoked if an exception occurs, or <see langword="null"/> if none.</param>
		/// <param name="taskSource">The <see cref="TaskCompletionSource{Object}"/> used to control the task's completion state.</param>
		/// <param name="syncContext">The synchronization context used to wrap callbacks, or <see langword="null"/> if none.</param>
		/// <param name="cancellationToken">The token used to cancel the task. Defaults to <see cref="CancellationToken.None"/>.</param>
		/// <returns>A new <see cref="TaskUnit"/> instance configured with the specified operation, callbacks, and task source.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncFunction"/> or <paramref name="taskSource"/> is <see langword="null"/>.</exception>
		public static TaskUnit Create(Func<Task> asyncFunction, Action completeCallback, Action cancelCallback, Action<Exception> failCallback, TaskCompletionSource<object> taskSource, SynchronizationContext syncContext = null, CancellationToken cancellationToken = default)
		{
			if (asyncFunction == null)
				throw new ArgumentNullException(nameof(asyncFunction));
			if (taskSource == null)
				throw new ArgumentNullException(nameof(taskSource));

			(completeCallback, cancelCallback, failCallback) = WrapCallbacks(completeCallback, cancelCallback, failCallback, syncContext);

			return new TaskUnit(async () =>
			{
				try
				{
					await asyncFunction();
					completeCallback?.Invoke();
					taskSource.SetResult(null);
				}
				catch (TaskCanceledException)
				{
					cancelCallback?.Invoke();
					taskSource.SetCanceled();
				}
				catch (OperationCanceledException)
				{
					cancelCallback?.Invoke();
					taskSource.SetCanceled();
				}
				catch (Exception e)
				{
					failCallback?.Invoke(e);
					taskSource.SetException(e);
				}
			}, cancellationToken, cancelCallback);
		}

		/// <summary>
		/// Creates a <see cref="TaskUnit"/> for a generic asynchronous operation with callbacks and provides a <see cref="Task{T}"/> for external tracking.
		/// </summary>
		/// <typeparam name="T">The type of the result returned by the asynchronous operation.</typeparam>
		/// <param name="asyncFunction">The asynchronous operation that returns a value of type <typeparamref name="T"/>.</param>
		/// <param name="completeCallback">The callback invoked with the result upon successful completion, or <see langword="null"/> if none.</param>
		/// <param name="cancelCallback">The callback invoked if the task is canceled, or <see langword="null"/> if none.</param>
		/// <param name="failCallback">The callback invoked if an exception occurs, or <see langword="null"/> if none.</param>
		/// <param name="task">When this method returns, contains the <see cref="Task{T}"/> representing the operation's completion state.</param>
		/// <param name="syncContext">The synchronization context used to wrap callbacks, or <see langword="null"/> if none.</param>
		/// <param name="cancellationToken">The token used to cancel the task. Defaults to <see cref="CancellationToken.None"/>.</param>
		/// <returns>A new <see cref="TaskUnit"/> instance configured with the specified operation, callbacks, and task tracking.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncFunction"/> is <see langword="null"/>.</exception>
		public static TaskUnit Create<T>(Func<Task<T>> asyncFunction, Action<T> completeCallback, Action cancelCallback, Action<Exception> failCallback, out Task<T> task, SynchronizationContext syncContext = null, CancellationToken cancellationToken = default)
		{
			if (asyncFunction == null)
				throw new ArgumentNullException(nameof(asyncFunction));

			var taskSource = new TaskCompletionSource<T>();
			task = taskSource.Task;

			(completeCallback, cancelCallback, failCallback) = WrapCallbacks(completeCallback, cancelCallback, failCallback, syncContext);

			return new TaskUnit(async () =>
			{
				try
				{
					var result = await asyncFunction();
					completeCallback?.Invoke(result);
					taskSource.SetResult(result);
				}
				catch (TaskCanceledException)
				{
					cancelCallback?.Invoke();
					taskSource.SetCanceled();
				}
				catch (OperationCanceledException)
				{
					cancelCallback?.Invoke();
					taskSource.SetCanceled();
				}
				catch (Exception e)
				{
					failCallback?.Invoke(e);
					taskSource.SetException(e);
				}
			}, cancellationToken, cancelCallback);
		}

		/// <summary>
		/// Creates a <see cref="TaskUnit"/> for a generic asynchronous operation with callbacks and a specified <see cref="TaskCompletionSource{T}"/>.
		/// </summary>
		/// <typeparam name="T">The type of the result returned by the asynchronous operation.</typeparam>
		/// <param name="asyncFunction">The asynchronous operation that returns a value of type <typeparamref name="T"/>.</param>
		/// <param name="completeCallback">The callback invoked with the result upon successful completion, or <see langword="null"/> if none.</param>
		/// <param name="cancelCallback">The callback invoked if the task is canceled, or <see langword="null"/> if none.</param>
		/// <param name="failCallback">The callback invoked if an exception occurs, or <see langword="null"/> if none.</param>
		/// <param name="taskSource">The <see cref="TaskCompletionSource{T}"/> used to control the task's completion state.</param>
		/// <param name="syncContext">The synchronization context used to wrap callbacks, or <see langword="null"/> if none.</param>
		/// <param name="cancellationToken">The token used to cancel the task. Defaults to <see cref="CancellationToken.None"/>.</param>
		/// <returns>A new <see cref="TaskUnit"/> instance configured with the specified operation, callbacks, and task source.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncFunction"/> or <paramref name="taskSource"/> is <see langword="null"/>.</exception>
		public static TaskUnit Create<T>(Func<Task<T>> asyncFunction, Action<T> completeCallback, Action cancelCallback, Action<Exception> failCallback, TaskCompletionSource<T> taskSource, SynchronizationContext syncContext = null, CancellationToken cancellationToken = default)
		{
			if (asyncFunction == null)
				throw new ArgumentNullException(nameof(asyncFunction));
			if (taskSource == null)
				throw new ArgumentNullException(nameof(taskSource));

			(completeCallback, cancelCallback, failCallback) = WrapCallbacks(completeCallback, cancelCallback, failCallback, syncContext);

			return new TaskUnit(async () =>
			{
				try
				{
					var result = await asyncFunction();
					completeCallback?.Invoke(result);
					taskSource.SetResult(result);
				}
				catch (TaskCanceledException)
				{
					cancelCallback?.Invoke();
					taskSource.SetCanceled();
				}
				catch (OperationCanceledException)
				{
					cancelCallback?.Invoke();
					taskSource.SetCanceled();
				}
				catch (Exception e)
				{
					failCallback?.Invoke(e);
					taskSource.SetException(e);
				}
			}, cancellationToken, cancelCallback);
		}
	}
}