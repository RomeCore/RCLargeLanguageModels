using System.Collections.Generic;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Tools;

namespace RCLargeLanguageModels.Messages
{
	/// <summary>
	/// Represents an assistant message, completed or partial.
	/// </summary>
	/// <remarks>
	/// See also: <para/>
	/// <see cref="AssistantMessage"/> <br/>
	/// <see cref="PartialAssistantMessage"/> <br/>
	/// </remarks>
	public interface IAssistantMessage : IAttachmentsMessage, ICompletion
	{
		/// <summary>
		/// The reasoning content of the assistant message. Can be <see langword="null"/>.
		/// </summary>
		/// <remarks>
		/// Includes thoughts and some interesting things for user, but should be excluded from message list in the API.
		/// </remarks>
		string? ReasoningContent { get; }

		/// <summary>
		/// Gets the list of tool calls.
		/// </summary>
		IReadOnlyList<IToolCall> ToolCalls { get; }
	}
}