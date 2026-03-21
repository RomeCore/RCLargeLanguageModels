using System.Collections.Generic;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Formats;
using RCLargeLanguageModels.Tools;
using RCLargeLanguageModels.Completions;
using System.Threading.Tasks;

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
		/// <param name="parameters">The parameters for injecting in the general completions.</param>
		Task InjectCompletionAsync(CompletionInjectionParameters parameters);

		/// <summary>
		/// Injects properties into LLM API execution process.
		/// </summary>
		/// <param name="parameters">The parameters for injecting in the chat completions.</param>
		Task InjectChatCompletionAsync(ChatCompletionInjectionParameters parameters);

		/// <summary>
		/// Injects properties into LLM API execution process.
		/// </summary>
		/// <param name="parameters">The parameters for injecting in the embeddings generations.</param>
		Task InjectEmbeddingAsync(EmbeddingInjectionParameters parameters);
	}
}