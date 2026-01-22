using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Tasks
{
	public static class TaskExtensions
	{
		public static bool IsCompletedSuccessfully(this Task task)
		{
			return task.IsCompleted && !task.IsCanceled && !task.IsFaulted;
		}

		public static async Task<T> WaitMax<T>(this Task<T> task, int delayMs)
		{
			if (task == null)
				throw new ArgumentNullException(nameof(task));

			var delayTask = Task.Delay(delayMs);
			var completed = await Task.WhenAny(task, delayTask);

			if (completed == delayTask)
				throw new TimeoutException($"Task did not complete within {delayMs} ms.");

			return await task;
		}
	}
}