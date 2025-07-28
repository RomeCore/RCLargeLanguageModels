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
	/// Specifies options for enqueuing tasks in a <see cref="TaskQueueMaster"/>
	/// </summary>
	public enum TaskQueueMode
	{
		/// <summary>
		/// Executes the task immediately without enqueuing, this is the default mode
		/// </summary>
		ExecuteImmediately = 0,

		/// <summary>
		/// Enqueues the task in the task queue with one parallel task
		/// </summary>
		EnqueueLevel1,

		/// <summary>
		/// Enqueues the task in the task queue with two parallel tasks
		/// </summary>
		EnqueueLevel2,

		/// <summary>
		/// Enqueues the task in the task queue with four parallel tasks
		/// </summary>
		EnqueueLevel4,

		/// <summary>
		/// Enqueues the task in the task queue with eight parallel tasks
		/// </summary>
		EnqueueLevel8,

		/// <summary>
		/// Enqueues the task in the task queue with sixteen parallel tasks
		/// </summary>
		EnqueueLevel16
	}

	/// <summary>
	/// Provides a high-level interface for enqueuing and managing asynchronous tasks in a <see cref="TaskQueue"/>.
	/// </summary>
	public static class TaskQueueMaster
	{
		private static readonly TaskQueue _queueLevel1 = new TaskQueue(maxDegreeOfParallelism: 1);
		private static readonly TaskQueue _queueLevel2 = new TaskQueue(maxDegreeOfParallelism: 2);
		private static readonly TaskQueue _queueLevel4 = new TaskQueue(maxDegreeOfParallelism: 4);
		private static readonly TaskQueue _queueLevel8 = new TaskQueue(maxDegreeOfParallelism: 8);
		private static readonly TaskQueue _queueLevel16 = new TaskQueue(maxDegreeOfParallelism: 16);

		/// <summary>
		/// A task queue with a maximum degree of parallelism of 1.
		/// </summary>
		public static TaskQueue QueueLevel1 => _queueLevel1;

		/// <summary>
		/// A task queue with a maximum degree of parallelism of 2.
		/// </summary>
		public static TaskQueue QueueLevel2 => _queueLevel2;

		/// <summary>
		/// A task queue with a maximum degree of parallelism of 4.
		/// </summary>
		public static TaskQueue QueueLevel4 => _queueLevel4;

		/// <summary>
		/// A task queue with a maximum degree of parallelism of 8.
		/// </summary>
		public static TaskQueue QueueLevel8 => _queueLevel8;

		/// <summary>
		/// A task queue with a maximum degree of parallelism of 16.
		/// </summary>
		public static TaskQueue QueueLevel16 => _queueLevel16;

		/// <summary>
		/// Gets the task queue based on the specified mode.
		/// </summary>
		/// <param name="mode">The mode to get the task queue for.</param>
		/// <returns>The task queue based on the specified mode. Or null if the mode is <see cref="TaskQueueMode.ExecuteImmediately"/>.</returns>
		/// <exception cref="ArgumentException">Thrown when the specified mode is invalid.</exception>
		public static TaskQueue GetQueue(TaskQueueMode mode)
		{
			switch (mode)
			{
				case TaskQueueMode.ExecuteImmediately:
					return null;

				case TaskQueueMode.EnqueueLevel1:
					return _queueLevel1;
				case TaskQueueMode.EnqueueLevel2:
					return _queueLevel2;
				case TaskQueueMode.EnqueueLevel4:
					return _queueLevel4;
				case TaskQueueMode.EnqueueLevel8:
					return _queueLevel8;
				case TaskQueueMode.EnqueueLevel16:
					return _queueLevel16;

				default:
					throw new ArgumentException($"Invalid {nameof(TaskQueueMode)}", nameof(mode));
			}
		}

		/// <summary>
		/// Enqueues a pre-configured <see cref="TaskUnit"/> into the appropriate queue based on the specified mode.
		/// </summary>
		/// <param name="mode">The mode specifying how the task should be executed or queued.</param>
		/// <param name="taskUnit">The <see cref="TaskUnit"/> to enqueue.</param>
		/// <param name="priority">The priority of the task, where higher values indicate higher priority (e.g., 5 is processed before 1). Defaults to 0.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="taskUnit"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="mode"/> is invalid.</exception>
		public static void Enqueue(TaskQueueMode mode, TaskUnit taskUnit, int priority = 0)
		{
			Enqueue(GetQueue(mode), taskUnit, priority);
		}

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
		/// <param name="mode">The mode specifying how the task should be executed or queued.</param>
		/// <param name="asyncFunction">The asynchronous operation to execute.</param>
		/// <param name="completeCallback">The callback invoked upon successful completion, or <see langword="null"/> if none.</param>
		/// <param name="cancelCallback">The callback invoked if the task is canceled, or <see langword="null"/> if none.</param>
		/// <param name="failCallback">The callback invoked if an exception occurs, or <see langword="null"/> if none.</param>
		/// <param name="priority">The priority of the task, where higher values indicate higher priority (e.g., 5 is processed before 1). Defaults to 0.</param>
		/// <param name="syncContext">The <see cref="SynchronizationContext"/> used to wrap callbacks, or <see langword="null"/> if none.</param>
		/// <param name="cancellationToken">The token used to cancel the task. Defaults to <see cref="CancellationToken.None"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncFunction"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="mode"/> is invalid.</exception>
		public static void Enqueue(
			TaskQueueMode mode,
			Func<Task> asyncFunction,
			Action completeCallback = null,
			Action cancelCallback = null,
			Action<Exception> failCallback = null,
			int priority = 0,
			SynchronizationContext syncContext = null,
			CancellationToken cancellationToken = default)
		{
			var taskUnit = TaskUnit.Create(asyncFunction, completeCallback, cancelCallback, failCallback, syncContext, cancellationToken);
			Enqueue(mode, taskUnit, priority);
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
		/// <param name="mode">The mode specifying how the task should be executed or queued.</param>
		/// <param name="asyncFunction">The asynchronous operation that returns a value of type <typeparamref name="T"/>.</param>
		/// <param name="completeCallback">The callback invoked with the result upon successful completion, or <see langword="null"/> if none.</param>
		/// <param name="cancelCallback">The callback invoked if the task is canceled, or <see langword="null"/> if none.</param>
		/// <param name="failCallback">The callback invoked if an exception occurs, or <see langword="null"/> if none.</param>
		/// <param name="priority">The priority of the task, where higher values indicate higher priority (e.g., 5 is processed before 1). Defaults to 0.</param>
		/// <param name="syncContext">The <see cref="SynchronizationContext"/> used to wrap callbacks, or <see langword="null"/> if none.</param>
		/// <param name="cancellationToken">The token used to cancel the task. Defaults to <see cref="CancellationToken.None"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncFunction"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="mode"/> is invalid.</exception>
		public static void Enqueue<T>(
			TaskQueueMode mode,
			Func<Task<T>> asyncFunction,
			Action<T> completeCallback = null,
			Action cancelCallback = null,
			Action<Exception> failCallback = null,
			int priority = 0,
			SynchronizationContext syncContext = null,
			CancellationToken cancellationToken = default)
		{
			var taskUnit = TaskUnit.Create(asyncFunction, completeCallback, cancelCallback, failCallback, syncContext, cancellationToken);
			Enqueue(mode, taskUnit, priority);
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
		/// <param name="mode">The mode specifying how the task should be executed or queued.</param>
		/// <param name="asyncFunction">The asynchronous operation to execute.</param>
		/// <param name="completeCallback">The callback invoked upon successful completion, or <see langword="null"/> if none.</param>
		/// <param name="cancelCallback">The callback invoked if the task is canceled, or <see langword="null"/> if none.</param>
		/// <param name="failCallback">The callback invoked if an exception occurs, or <see langword="null"/> if none.</param>
		/// <param name="priority">The priority of the task, where higher values indicate higher priority (e.g., 5 is processed before 1). Defaults to 0.</param>
		/// <param name="syncContext">The <see cref="SynchronizationContext"/> used to wrap callbacks, or <see langword="null"/> if none.</param>
		/// <param name="cancellationToken">The token used to cancel the task. Defaults to <see cref="CancellationToken.None"/>.</param>
		/// <returns>A <see cref="Task"/> representing the completion of the enqueued operation.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncFunction"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="mode"/> is invalid.</exception>
		public static Task EnqueueAsync(
			TaskQueueMode mode,
			Func<Task> asyncFunction,
			Action completeCallback = null,
			Action cancelCallback = null,
			Action<Exception> failCallback = null,
			int priority = 0,
			SynchronizationContext syncContext = null,
			CancellationToken cancellationToken = default)
		{
			var taskUnit = TaskUnit.Create(asyncFunction, completeCallback, cancelCallback, failCallback, out var task, syncContext, cancellationToken);
			Enqueue(mode, taskUnit, priority);
			return task;
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
		/// <param name="mode">The mode specifying how the task should be executed or queued.</param>
		/// <param name="asyncFunction">The asynchronous operation that returns a value of type <typeparamref name="T"/>.</param>
		/// <param name="completeCallback">The callback invoked with the result upon successful completion, or <see langword="null"/> if none.</param>
		/// <param name="cancelCallback">The callback invoked if the task is canceled, or <see langword="null"/> if none.</param>
		/// <param name="failCallback">The callback invoked if an exception occurs, or <see langword="null"/> if none.</param>
		/// <param name="priority">The priority of the task, where higher values indicate higher priority (e.g., 5 is processed before 1). Defaults to 0.</param>
		/// <param name="syncContext">The <see cref="SynchronizationContext"/> used to wrap callbacks, or <see langword="null"/> if none.</param>
		/// <param name="cancellationToken">The token used to cancel the task. Defaults to <see cref="CancellationToken.None"/>.</param>
		/// <returns>A <see cref="Task{T}"/> representing the completion of the enqueued operation.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="asyncFunction"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="mode"/> is invalid.</exception>
		public static Task<T> EnqueueAsync<T>(
			TaskQueueMode mode,
			Func<Task<T>> asyncFunction,
			Action<T> completeCallback = null,
			Action cancelCallback = null,
			Action<Exception> failCallback = null,
			int priority = 0,
			SynchronizationContext syncContext = null,
			CancellationToken cancellationToken = default)
		{
			var taskUnit = TaskUnit.Create(asyncFunction, completeCallback, cancelCallback, failCallback, out var task, syncContext, cancellationToken);
			Enqueue(mode, taskUnit, priority);
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