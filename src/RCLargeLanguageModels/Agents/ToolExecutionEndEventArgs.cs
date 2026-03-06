using System;
using System.Threading.Tasks;
using RCLargeLanguageModels.Tools;

namespace RCLargeLanguageModels.Agents
{
	/// <summary>
	/// Event arguments for tool execution end event.
	/// </summary>
	public class ToolExecutionEndEventArgs : EventArgs
	{
		public ITool Tool { get; }
		public IToolCall ToolCall { get; }
		public Task<ToolResult> ResultTask { get; }
		public ToolResult? Result { get; set; }

		public ToolExecutionEndEventArgs(ITool tool, IToolCall toolCall, Task<ToolResult> resultTask, ToolResult? result = null)
		{
			Tool = tool;
			ToolCall = toolCall;
			ResultTask = resultTask;
			Result = result;
		}
	}
}