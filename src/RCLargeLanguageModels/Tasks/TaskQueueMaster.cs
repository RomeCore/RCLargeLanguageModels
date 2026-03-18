using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Tasks
{
	// Ухх бляя, этот класс надо переписать
	// (c) Cody

	/// <summary>
	/// Provides a high-level interface for enqueuing and managing asynchronous tasks in a <see cref="TaskQueue"/>.
	/// </summary>
	public static class TaskQueueMaster
	{
		/// <summary>
		/// Enqueues a pre-configured <see cref="TaskUnit"/> into the appropriate queue based on the specified queue.
		/// </summary>
		/// <param name="queue">The <see cref="TaskQueue"/> to enqueue the task into. If <see langword="null"/>, the task will be executed immediately.</param>
		/// <param name="taskUnit">The <see cref="TaskUnit"/> to enqueue.</param>
		/// <param name="priority">The priority of the task, where higher values indicate higher priority (e.g., 5 is processed before 1). Defaults to 0.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="taskUnit"/> is <see langword="null"/>.</exception>
		public static void Enqueue(TaskQueue queue, TaskUnit taskUnit, int priority = 0)
		{
			if (taskUnit == null)
				throw new ArgumentNullException(nameof(taskUnit));

			if (queue == null)
				Task.Run(taskUnit.TaskFactory);
			else
				queue.Enqueue(taskUnit, priority);
		}

		/// <summary>
		/// Enqueues a pre-configured <see cref="TaskUnit"/> into the appropriate queue based on the specified queue.
		/// </summary>
		/// <param name="queueParameters">The task queue parameters that contains the target queue and priority.</param>
		/// <param name="taskUnit">The <see cref="TaskUnit"/> to enqueue.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="taskUnit"/> is <see langword="null"/>.</exception>
		public static void Enqueue(TaskQueueParameters queueParameters, TaskUnit taskUnit)
		{
			if (queueParameters == null)
				throw new ArgumentNullException(nameof(queueParameters));
			if (taskUnit == null)
				throw new ArgumentNullException(nameof(taskUnit));

			if (queueParameters.EnqueueInto == null)
				Task.Run(taskUnit.TaskFactory);
			else
				queueParameters.EnqueueInto.Enqueue(taskUnit, queueParameters.EnqueuePriority);
		}

		/// <summary>
		/// Enqueues a non-generic asynchronous task with optional callbacks.
		/// </summary>
		/// <param name="queue">The <see cref="TaskQueue"/> to enqueue the task into. If <see langword="null"/>, the task will be executed immediately.</param>
		/// <param name="asyncFunction">The asynchronous operation to execute.</param>
		/// <param name="completeCallback">The callback invoked upon successful completion, or <see langword="null"/> if none.</param>
		/// <param name="cancelCallback">The callback invoked if the task is canceled, or <see langword="null"/> if none.</param>
		/// <param name="failCallback">The callback invoked if an exception occurs, or <see langword="null"/> if none.</param>
		/// <param name="priority">The priority of the task, where higher values indicate higher priority (e.g., 5 is processed before 1). Defaults to 0.</param>
		/// <param name="syncContext">The <see cref="SynchronizationContext"/> used to wrap callbacks, or <see langword="null"/> if none.</param>
		/// <param name="cancellationToken">The token used to cancel the task. Defaults to <see cref="CancellationToken.None"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncFunction"/> is <see langword="null"/>.</exception>
		public static void Enqueue(
			TaskQueue queue,
			Func<Task> asyncFunction,
			Action completeCallback = null,
			Action cancelCallback = null,
			Action<Exception> failCallback = null,
			int priority = 0,
			SynchronizationContext syncContext = null,
			CancellationToken cancellationToken = default)
		{
			var taskUnit = TaskUnit.Create(asyncFunction, completeCallback, cancelCallback, failCallback, syncContext, cancellationToken);
			Enqueue(queue, taskUnit, priority);
		}

		/// <summary>
		/// Enqueues a non-generic asynchronous task with optional callbacks.
		/// </summary>
		/// <param name="queueParameters">The task queue parameters that contains the target queue and priority.</param>
		/// <param name="asyncFunction">The asynchronous operation to execute.</param>
		/// <param name="completeCallback">The callback invoked upon successful completion, or <see langword="null"/> if none.</param>
		/// <param name="cancelCallback">The callback invoked if the task is canceled, or <see langword="null"/> if none.</param>
		/// <param name="failCallback">The callback invoked if an exception occurs, or <see langword="null"/> if none.</param>
		/// <param name="syncContext">The <see cref="SynchronizationContext"/> used to wrap callbacks, or <see langword="null"/> if none.</param>
		/// <param name="cancellationToken">The token used to cancel the task. Defaults to <see cref="CancellationToken.None"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncFunction"/> is <see langword="null"/>.</exception>
		public static void Enqueue(
			TaskQueueParameters queueParameters,
			Func<Task> asyncFunction,
			Action completeCallback = null,
			Action cancelCallback = null,
			Action<Exception> failCallback = null,
			SynchronizationContext syncContext = null,
			CancellationToken cancellationToken = default)
		{
			if (queueParameters == null)
				throw new ArgumentNullException(nameof(queueParameters));

			var taskUnit = TaskUnit.Create(asyncFunction, completeCallback, cancelCallback, failCallback, syncContext, cancellationToken);
			Enqueue(queueParameters, taskUnit);
		}

		/// <summary>
		/// Enqueues a generic asynchronous task with optional callbacks.
		/// </summary>
		/// <typeparam name="T">The type of the result returned by the asynchronous operation.</typeparam>
		/// <param name="queue">The <see cref="TaskQueue"/> to enqueue the task into. If <see langword="null"/>, the task will be executed immediately.</param>
		/// <param name="asyncFunction">The asynchronous operation that returns a value of type <typeparamref name="T"/>.</param>
		/// <param name="completeCallback">The callback invoked with the result upon successful completion, or <see langword="null"/> if none.</param>
		/// <param name="cancelCallback">The callback invoked if the task is canceled, or <see langword="null"/> if none.</param>
		/// <param name="failCallback">The callback invoked if an exception occurs, or <see langword="null"/> if none.</param>
		/// <param name="priority">The priority of the task, where higher values indicate higher priority (e.g., 5 is processed before 1). Defaults to 0.</param>
		/// <param name="syncContext">The <see cref="SynchronizationContext"/> used to wrap callbacks, or <see langword="null"/> if none.</param>
		/// <param name="cancellationToken">The token used to cancel the task. Defaults to <see cref="CancellationToken.None"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncFunction"/> is <see langword="null"/>.</exception>
		public static void Enqueue<T>(
			TaskQueue queue,
			Func<Task<T>> asyncFunction,
			Action<T> completeCallback = null,
			Action cancelCallback = null,
			Action<Exception> failCallback = null,
			int priority = 0,
			SynchronizationContext syncContext = null,
			CancellationToken cancellationToken = default)
		{
			var taskUnit = TaskUnit.Create(asyncFunction, completeCallback, cancelCallback, failCallback, syncContext, cancellationToken);
			Enqueue(queue, taskUnit, priority);
		}

		/// <summary>
		/// Enqueues a generic asynchronous task with optional callbacks.
		/// </summary>
		/// <typeparam name="T">The type of the result returned by the asynchronous operation.</typeparam>
		/// <param name="queueParameters">The task queue parameters that contains the target queue and priority.</param>
		/// <param name="asyncFunction">The asynchronous operation that returns a value of type <typeparamref name="T"/>.</param>
		/// <param name="completeCallback">The callback invoked with the result upon successful completion, or <see langword="null"/> if none.</param>
		/// <param name="cancelCallback">The callback invoked if the task is canceled, or <see langword="null"/> if none.</param>
		/// <param name="failCallback">The callback invoked if an exception occurs, or <see langword="null"/> if none.</param>
		/// <param name="syncContext">The <see cref="SynchronizationContext"/> used to wrap callbacks, or <see langword="null"/> if none.</param>
		/// <param name="cancellationToken">The token used to cancel the task. Defaults to <see cref="CancellationToken.None"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncFunction"/> is <see langword="null"/>.</exception>
		public static void Enqueue<T>(
			TaskQueueParameters queueParameters,
			Func<Task<T>> asyncFunction,
			Action<T> completeCallback = null,
			Action cancelCallback = null,
			Action<Exception> failCallback = null,
			SynchronizationContext syncContext = null,
			CancellationToken cancellationToken = default)
		{
			if (queueParameters == null)
				throw new ArgumentNullException(nameof(queueParameters));

			var taskUnit = TaskUnit.Create(asyncFunction, completeCallback, cancelCallback, failCallback, syncContext, cancellationToken);
			Enqueue(queueParameters, taskUnit);
		}

		/// <summary>
		/// Enqueues a non-generic asynchronous task with callbacks and provides a <see cref="Task"/> for external tracking.
		/// </summary>
		/// <param name="queue">The <see cref="TaskQueue"/> to enqueue the task into. If <see langword="null"/>, the task will be executed immediately.</param>
		/// <param name="asyncFunction">The asynchronous operation to execute.</param>
		/// <param name="completeCallback">The callback invoked upon successful completion, or <see langword="null"/> if none.</param>
		/// <param name="cancelCallback">The callback invoked if the task is canceled, or <see langword="null"/> if none.</param>
		/// <param name="failCallback">The callback invoked if an exception occurs, or <see langword="null"/> if none.</param>
		/// <param name="priority">The priority of the task, where higher values indicate higher priority (e.g., 5 is processed before 1). Defaults to 0.</param>
		/// <param name="syncContext">The <see cref="SynchronizationContext"/> used to wrap callbacks, or <see langword="null"/> if none.</param>
		/// <param name="cancellationToken">The token used to cancel the task. Defaults to <see cref="CancellationToken.None"/>.</param>
		/// <returns>A <see cref="Task"/> representing the completion of the enqueued operation.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncFunction"/> is <see langword="null"/>.</exception>
		public static Task EnqueueAsync(
			TaskQueue queue,
			Func<Task> asyncFunction,
			Action completeCallback = null,
			Action cancelCallback = null,
			Action<Exception> failCallback = null,
			int priority = 0,
			SynchronizationContext syncContext = null,
			CancellationToken cancellationToken = default)
		{
			var taskUnit = TaskUnit.Create(asyncFunction, completeCallback, cancelCallback, failCallback, out var task, syncContext, cancellationToken);
			Enqueue(queue, taskUnit, priority);
			return task;
		}

		/// <summary>
		/// Enqueues a non-generic asynchronous task with callbacks and provides a <see cref="Task"/> for external tracking.
		/// </summary>
		/// <param name="queueParameters">The task queue parameters that contains the target queue and priority.</param>
		/// <param name="asyncFunction">The asynchronous operation to execute.</param>
		/// <param name="completeCallback">The callback invoked upon successful completion, or <see langword="null"/> if none.</param>
		/// <param name="cancelCallback">The callback invoked if the task is canceled, or <see langword="null"/> if none.</param>
		/// <param name="failCallback">The callback invoked if an exception occurs, or <see langword="null"/> if none.</param>
		/// <param name="syncContext">The <see cref="SynchronizationContext"/> used to wrap callbacks, or <see langword="null"/> if none.</param>
		/// <param name="cancellationToken">The token used to cancel the task. Defaults to <see cref="CancellationToken.None"/>.</param>
		/// <returns>A <see cref="Task"/> representing the completion of the enqueued operation.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncFunction"/> is <see langword="null"/>.</exception>
		public static Task EnqueueAsync(
			TaskQueueParameters queueParameters,
			Func<Task> asyncFunction,
			Action completeCallback = null,
			Action cancelCallback = null,
			Action<Exception> failCallback = null,
			SynchronizationContext syncContext = null,
			CancellationToken cancellationToken = default)
		{
			if (queueParameters == null)
				throw new ArgumentNullException(nameof(queueParameters));

			var taskUnit = TaskUnit.Create(asyncFunction, completeCallback, cancelCallback, failCallback, out var task, syncContext, cancellationToken);
			Enqueue(queueParameters, taskUnit);
			return task;
		}

		/// <summary>
		/// Enqueues a generic asynchronous task with callbacks and provides a <see cref="Task{T}"/> for external tracking.
		/// </summary>
		/// <typeparam name="T">The type of the result returned by the asynchronous operation.</typeparam>
		/// <param name="queue">The <see cref="TaskQueue"/> to which the task should be enqueued.</param>
		/// <param name="asyncFunction">The asynchronous operation that returns a value of type <typeparamref name="T"/>.</param>
		/// <param name="completeCallback">The callback invoked with the result upon successful completion, or <see langword="null"/> if none.</param>
		/// <param name="cancelCallback">The callback invoked if the task is canceled, or <see langword="null"/> if none.</param>
		/// <param name="failCallback">The callback invoked if an exception occurs, or <see langword="null"/> if none.</param>
		/// <param name="priority">The priority of the task, where higher values indicate higher priority (e.g., 5 is processed before 1). Defaults to 0.</param>
		/// <param name="syncContext">The <see cref="SynchronizationContext"/> used to wrap callbacks, or <see langword="null"/> if none.</param>
		/// <param name="cancellationToken">The token used to cancel the task. Defaults to <see cref="CancellationToken.None"/>.</param>
		/// <returns>A <see cref="Task{T}"/> representing the completion of the enqueued operation.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncFunction"/> is <see langword="null"/>.</exception>
		public static Task<T> EnqueueAsync<T>(
			TaskQueue queue,
			Func<Task<T>> asyncFunction,
			Action<T> completeCallback = null,
			Action cancelCallback = null,
			Action<Exception> failCallback = null,
			int priority = 0,
			SynchronizationContext syncContext = null,
			CancellationToken cancellationToken = default)
		{
			var taskUnit = TaskUnit.Create(asyncFunction, completeCallback, cancelCallback, failCallback, out var task, syncContext, cancellationToken);
			Enqueue(queue, taskUnit, priority);
			return task;
		}

		/// <summary>
		/// Enqueues a generic asynchronous task with callbacks and provides a <see cref="Task{T}"/> for external tracking.
		/// </summary>
		/// <typeparam name="T">The type of the result returned by the asynchronous operation.</typeparam>
		/// <param name="queueParameters">The task queue parameters that contains the target queue and priority.</param>
		/// <param name="asyncFunction">The asynchronous operation that returns a value of type <typeparamref name="T"/>.</param>
		/// <param name="completeCallback">The callback invoked with the result upon successful completion, or <see langword="null"/> if none.</param>
		/// <param name="cancelCallback">The callback invoked if the task is canceled, or <see langword="null"/> if none.</param>
		/// <param name="failCallback">The callback invoked if an exception occurs, or <see langword="null"/> if none.</param>
		/// <param name="syncContext">The <see cref="SynchronizationContext"/> used to wrap callbacks, or <see langword="null"/> if none.</param>
		/// <param name="cancellationToken">The token used to cancel the task. Defaults to <see cref="CancellationToken.None"/>.</param>
		/// <returns>A <see cref="Task{T}"/> representing the completion of the enqueued operation.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncFunction"/> is <see langword="null"/>.</exception>
		public static Task<T> EnqueueAsync<T>(
			TaskQueueParameters queueParameters,
			Func<Task<T>> asyncFunction,
			Action<T> completeCallback = null,
			Action cancelCallback = null,
			Action<Exception> failCallback = null,
			SynchronizationContext syncContext = null,
			CancellationToken cancellationToken = default)
		{
			if (queueParameters == null)
				throw new ArgumentNullException(nameof(queueParameters));

			var taskUnit = TaskUnit.Create(asyncFunction, completeCallback, cancelCallback, failCallback, out var task, syncContext, cancellationToken);
			Enqueue(queueParameters, taskUnit);
			return task;
		}
	}
}