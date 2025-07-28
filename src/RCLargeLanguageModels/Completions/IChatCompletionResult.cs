using System.Collections.Generic;
using RCLargeLanguageModels.Messages;

namespace RCLargeLanguageModels.Completions
{
	/// <summary>
	/// Represents a LLM-generated chat completion result.
	/// </summary>
	public interface IChatCompletionResult : ICompletionResult
	{
		/// <summary>
		/// Gets the first generated message from the list of available choices.
		/// </summary>
		IAssistantMessage Message { get; }

		/// <summary>
		/// Gets the list of available message completion choices. Will contain at least one message.
		/// </summary>
		new IReadOnlyList<IAssistantMessage> Choices { get; }
	}
}