using System;
using RCLargeLanguageModels.Tools;

namespace RCLargeLanguageModels.Agents
{
	/// <summary>
	/// Event arguments for tool execution begin event.
	/// </summary>
	public class ToolExecutionBeginEventArgs : EventArgs
	{
		public ITool Tool { get; }
		public IToolCall ToolCall { get; }
		public ToolResult? Result { get; set; }

		public ToolExecutionBeginEventArgs(ITool tool, IToolCall toolCall)
		{
			Tool = tool;
			ToolCall = toolCall;
		}
	}
}