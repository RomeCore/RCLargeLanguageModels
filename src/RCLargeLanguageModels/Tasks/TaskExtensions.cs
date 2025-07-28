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
	}
}