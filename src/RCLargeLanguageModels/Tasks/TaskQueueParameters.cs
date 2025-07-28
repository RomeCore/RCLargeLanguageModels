namespace RCLargeLanguageModels.Tasks
{
	/// <summary>
	/// Represents a parameters for enqueuing tasks and requests.
	/// </summary>
	public class TaskQueueParameters
	{
		/// <summary>
		/// Gets the task queue that used to enqueue requests. Can be null for executing immediately.
		/// </summary>
		public TaskQueue EnqueueInto { get; }

		/// <summary>
		/// Gets the priority of the requests when enqueued into a task queue. Higher values have higher priority.
		/// </summary>
		public int EnqueuePriority { get; }

		/// <summary>
		/// Gets a value indicating whether the model requests is executed immediately.
		/// </summary>
		public bool IsExecutedImmediately => EnqueueInto == null;

		/// <summary>
		/// Creates a copied instance with the specified priority.
		/// </summary>
		/// <param name="priority">The priority of the requests when enqueued into a task queue. Higher values have higher priority.</param>
		/// <returns>The <see cref="TaskQueueParameters"/> instance.</returns>
		public TaskQueueParameters WithPriority(int priority)
		{
			return new TaskQueueParameters(EnqueueInto, priority);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TaskQueueParameters"/> class with the specified task queue.
		/// </summary>
		/// <param name="enqueueInto">The task queue that used to enqueue requests. Can be null for executing immediately.</param>
		public TaskQueueParameters(TaskQueue enqueueInto)
		{
			EnqueueInto = enqueueInto;
			EnqueuePriority = 0;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TaskQueueParameters"/> class with the specified task queue and priority.
		/// </summary>
		/// <param name="enqueueInto">The task queue that used to enqueue requests. Can be null for executing immediately.</param>
		/// <param name="enqueuePriority">The priority of the requests when enqueued into a task queue. Higher values have higher priority.</param>
		public TaskQueueParameters(TaskQueue enqueueInto, int enqueuePriority)
		{
			EnqueueInto = enqueueInto;
			EnqueuePriority = enqueuePriority;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TaskQueueParameters"/> class with the specified task queue mode.
		/// </summary>
		/// <param name="mode">The task queue mode that used to enqueue requests.</param>
		public TaskQueueParameters(TaskQueueMode mode)
		{
			EnqueueInto = TaskQueueMaster.GetQueue(mode);
			EnqueuePriority = 0;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TaskQueueParameters"/> class with the specified task queue mode and priority.
		/// </summary>
		/// <param name="mode">The task queue mode that used to enqueue requests.</param>
		/// <param name="enqueuePriority">The priority of the requests when enqueued into a task queue. Higher values have higher priority.</param>
		public TaskQueueParameters(TaskQueueMode mode, int enqueuePriority)
		{
			EnqueueInto = TaskQueueMaster.GetQueue(mode);
			EnqueuePriority = enqueuePriority;
		}

		public static readonly TaskQueueParameters ExecuteImmediately = new TaskQueueParameters(TaskQueueMode.ExecuteImmediately);
	}
}