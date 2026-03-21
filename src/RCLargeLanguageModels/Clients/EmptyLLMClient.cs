using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Embeddings;
using RCLargeLanguageModels.Formats;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tools;

namespace RCLargeLanguageModels.Clients
{
	/// <summary>
	/// Represents an empty implementation of the <see cref="LLMClient"/>. This class can be used as a placeholder or for testing purposes.
	/// </summary>
	public class EmptyLLMClient : LLMClient
	{
		public override string Name => "empty";

		protected override Task<LLModelDescriptor[]> ListModelsOverrideAsync(CancellationToken cancellationToken)
		{
			return Task.FromResult(Array.Empty<LLModelDescriptor>());
		}

		protected override Task<ChatCompletionResult> CreateChatCompletionsOverrideAsync(
			LLModelDescriptor model,
			List<IMessage> messages,
			int count,
			List<CompletionProperty> properties,
			OutputFormatDefinition outputFormatDefinition,
			ToolSet tools,
			CancellationToken cancellationToken)
		{
			return Task.FromResult(new ChatCompletionResult(this, model, new AssistantMessage("Empty content")));
		}

		protected override Task<CompletionResult> CreateCompletionsOverrideAsync(
			LLModelDescriptor model,
			string prompt,
			string? suffix,
			int count,
			List<CompletionProperty> properties,
			CancellationToken cancellationToken)
		{
			return Task.FromResult(new CompletionResult(this, model, new Completion("Empty content")));
		}

		protected override Task<PartialChatCompletionResult> CreateStreamingChatCompletionsOverrideAsync(
			LLModelDescriptor model,
			List<IMessage> messages,
			int count,
			List<CompletionProperty> properties,
			OutputFormatDefinition outputFormatDefinition,
			ToolSet tools,
			CancellationToken cancellationToken)
		{
			var partialMessage = new PartialAssistantMessage();
			partialMessage.Complete();
			return Task.FromResult(new PartialChatCompletionResult(this, model, partialMessage));
		}

		protected override Task<PartialCompletionResult> CreateStreamingCompletionsOverrideAsync(
			LLModelDescriptor model,
			string prompt,
			string? suffix,
			int count,
			List<CompletionProperty> properties,
			CancellationToken cancellationToken)
		{
			var partialCompletion = new PartialCompletion();
			partialCompletion.Complete();
			return Task.FromResult(new PartialCompletionResult(this, model, partialCompletion));
		}

		protected override Task<EmbeddingResult> CreateEmbeddingsOverrideAsync(
			LLModelDescriptor model,
			List<string> inputs,
			List<CompletionProperty> properties,
			CancellationToken cancellationToken)
		{
			return Task.FromResult(new EmbeddingResult(this, model, new Embedding(new float[] { 1.0f })));
		}
	}
}