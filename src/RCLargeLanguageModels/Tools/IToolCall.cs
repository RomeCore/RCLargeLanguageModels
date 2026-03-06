using System;
using System.Text.Json.Nodes;

namespace RCLargeLanguageModels.Tools
{
	/// <summary>
	/// Represents a LLM tool call.
	/// </summary>
	public interface IToolCall
	{
		/// <summary>
		/// The tool call id, may be used later to put it into "tool" message, so LLM can identify the call origin.
		/// </summary>
		string Id { get; }

		/// <summary>
		/// The original tool name that was called.
		/// </summary>
		string ToolName { get; }
	}
}