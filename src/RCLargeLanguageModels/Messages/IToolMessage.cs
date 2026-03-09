using System;
using System.Collections.Generic;
using System.Text;
using RCLargeLanguageModels.Tools;

namespace RCLargeLanguageModels.Messages
{
	/// <summary>
	/// Represents a tool message that can be used to answer to the tool call.
	/// </summary>
	public interface IToolMessage : IAttachmentsMessage
	{
		/// <summary>
		/// The result of the tool execution.
		/// </summary>
		ToolResult Result { get; }

		/// <summary>
		/// The status of the tool execution.
		/// </summary>
		ToolResultStatus Status { get; }

		/// <summary>
		/// The tool call identifier used to response to the right tool call.
		/// Used by most LLMs to bind this message to the respective tool call.
		/// </summary>
		string ToolCallId { get; }

		/// <summary>
		/// The source tool name that is used to reponse to the right tool call.
		/// Used by some LLMs for understanding what the tool call is this message for.
		/// </summary>
		string ToolName { get; }
	}
}