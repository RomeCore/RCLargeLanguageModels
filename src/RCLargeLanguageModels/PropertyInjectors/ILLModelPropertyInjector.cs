using System.Collections.Generic;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Formats;
using RCLargeLanguageModels.Tools;
using RCLargeLanguageModels.Completions;

namespace RCLargeLanguageModels.PropertyInjectors
{
	/// <summary>
	/// An injector for LLM properties that will be called before the LLM is used.
	/// </summary>
	public interface ILLModelPropertyInjector
	{
		/// <summary>
		/// Injects properties into LLM API execution process.
		/// </summary>
		/// <param name="model">The model that uses injector.</param>
		/// <param name="prompt">The prompt to complete.</param>
		/// <param name="suffix">The suffix to use in fill-in-the-middle completions.</param>
		/// <param name="count">The count of completions to create.</param>
		/// <param name="completionProperties">The completion properties.</param>
		void InjectCompletion(
			LLModel model,
			ref string prompt,
			ref string? suffix,
			ref int count,
			ref IEnumerable<CompletionProperty>? completionProperties);

		/// <summary>
		/// Injects properties into LLM API execution process.
		/// </summary>
		/// <param name="model">The model that uses injector.</param>
		/// <param name="messages">The messages history.</param>
		/// <param name="count">The count of completions to create.</param>
		/// <param name="chatProperties">The chat completion properties.</param>
		/// <param name="tools">The tool set.</param>
		/// <param name="outputFormatDefinition">The native output format definition.</param>
		void InjectChatCompletion(
			LLModel model,
			ref IEnumerable<IMessage> messages,
			ref int count,
			ref IEnumerable<CompletionProperty>? chatProperties,
			ref IEnumerable<ITool>? tools,
			ref OutputFormatDefinition? outputFormatDefinition);
	}
}