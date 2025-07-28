using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace RCLargeLanguageModels.Tasks
{
	/// <summary>
	/// Manages a priority queue of asynchronous tasks, processing them with configurable concurrency
	/// </summary>
	public class TaskQueue
	{
		private readonly PriorityQueue<TaskUnit, int> _queue = new PriorityQueue<TaskUnit, int>(
			Comparer<int>.Create((a, b) => b.CompareTo(a)) // Higher values = higher priority
		);
		private readonly SemaphoreSlim _queueLock;
		private readonly SemaphoreSlim _processingSemaphore;
		private readonly int _maxDegreeOfParallelism;

		/// <summary>
		/// Initializes a new instance of the <see cref="TaskQueue"/> class with the specified maximum degree of parallelism
		/// </summary>
		/// <param name="maxDegreeOfParallelism">The maximum number of tasks that can be processed concurrently. Must be greater than zero</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxDegreeOfParallelism"/> is less than or equal to zero</exception>
		public TaskQueue(int maxDegreeOfParallelism = 1)
		{
			if (maxDegreeOfParallelism <= 0)
				throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), "Maximum degree of parallelism must be greater than zero.");

			_queueLock = new SemaphoreSlim(1, 1);
			_maxDegreeOfParallelism = maxDegreeOfParallelism;
			_processingSemaphore = new SemaphoreSlim(maxDegreeOfParallelism, maxDegreeOfParallelism);
		}

		/// <summary>
		/// Adds a <see cref="TaskUnit"/> to the queue with the specified priority
		/// </summary>
		/// <param name="taskUnit">The <see cref="TaskUnit"/> to enqueue</param>
		/// <param name="priority">The priority of the task, where higher values indicate higher priority (e.g., 5 is processed before 1). Defaults to 0</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="taskUnit"/> is <see langword="null"/></exception>
		public void Enqueue(TaskUnit taskUnit, int priority = 0)
		{
			if (taskUnit == null)
				throw new ArgumentNullException(nameof(taskUnit));

			_queueLock.Wait();
			try
			{
				_queue.Enqueue(taskUnit, priority);
			}
			finally
			{
				_queueLock.Release();
			}

			Task.Run(() => ProcessQueueAsync());
		}

		private async Task ProcessQueueAsync()
		{
			await _processingSemaphore.WaitAsync();
			try
			{
				TaskUnit taskUnit;

				await _queueLock.WaitAsync();

				try
				{
					if (_queue.Count == 0)
						return;

					taskUnit = _queue.Dequeue();
				}
				finally
				{
					_queueLock.Release();
				}

				if (taskUnit.CancellationToken.IsCancellationRequested)
				{
					taskUnit.OnCancel?.Invoke();
					return;
				}

				await taskUnit.TaskFactory();
			}
			catch (Exception ex)
			{
				Log.Error($"Queue processing error: {ex.Message}");
			}
			finally
			{
				_processingSemaphore.Release();
			}
		}
	}
}