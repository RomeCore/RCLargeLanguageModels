using System.Collections.Generic;
using RCLargeLanguageModels.Messages;

namespace RCLargeLanguageModels.Completions
{
	/// <summary>
	/// Represents a LLM-generated chat completion result.
	/// </summary>
	public interface IChatCompletionResult : IGenerationResult<IAssistantMessage>
	{
	}
}