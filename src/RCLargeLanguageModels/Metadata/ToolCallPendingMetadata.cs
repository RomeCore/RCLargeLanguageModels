using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Metadata
{
	/// <summary>
	/// Represents metadata that indicates that tool call is pending in streaming assistant message.
	/// This should be emitted once when tool call is started generating by LLM.
	/// </summary>
	public class ToolCallPendingMetadata : IPartialCompletionMetadata
	{
		/// <summary>
		/// Gets the name of the tool that is pending a call.
		/// </summary>
		public string ToolName { get; }

		public ToolCallPendingMetadata(string toolName)
		{
			ToolName = toolName;
		}
	}
}