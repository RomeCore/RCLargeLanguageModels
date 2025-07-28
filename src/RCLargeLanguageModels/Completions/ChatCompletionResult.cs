using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Metadata;
using RCLargeLanguageModels.Tasks;

namespace RCLargeLanguageModels.Completions
{
	/// <summary>
	/// Represents a completed chat completion result.
	/// </summary>
	public class ChatCompletionResult : IChatCompletionResult
	{
		/// <inheritdoc/>
		public LLMClient Client { get; }

		/// <inheritdoc/>
		public LLModelDescriptor Model { get; }

		/// <inheritdoc cref="IChatCompletionResult.Choices"/>
		public ImmutableArray<AssistantMessage> Choices { get; }
		IReadOnlyList<IAssistantMessage> IChatCompletionResult.Choices => Choices;
		IReadOnlyList<ICompletion> ICompletionResult.Choices => Choices;

		/// <inheritdoc cref="IChatCompletionResult.Message"/>
		public AssistantMessage Message => Choices[0];
		IAssistantMessage IChatCompletionResult.Message => Message;
		ICompletion ICompletionResult.Completion => Message;

		/// <inheritdoc/>
		public string? Content => Message.Content;

		/// <inheritdoc cref="ICompletionResult.Metadata"/>
		public MetadataCollection Metadata { get; }
		IMetadataCollection IMetadataProvider.Metadata => Metadata;
		IMetadataCollection ICompletionResult.Metadata => Metadata;

		/// <inheritdoc/>
		public IUsageMetadata UsageMetadata => Metadata?.TryGet<IUsageMetadata>();

		/// <summary>
		/// Creates a new instance of <see cref="ChatCompletionResult"/> using specified parameters.
		/// </summary>
		/// <param name="client">The client that used to generate this completion.</param>
		/// <param name="model">The model descriptor that used to generate this completion.</param>
		/// <param name="choice">The completion choice.</param>
		/// <param name="metadata">The collection of completion metadata, such as usage stats.</param>
		public ChatCompletionResult(
			LLMClient client,
			LLModelDescriptor model,
			AssistantMessage choice,
			IEnumerable<IMetadata>? metadata = null)
		{
			if (choice == null)
				throw new ArgumentNullException(nameof(choice));

			Client = client ?? throw new ArgumentNullException(nameof(client));
			Model = model ?? throw new ArgumentNullException(nameof(model));
			Choices = new AssistantMessage[] { choice }.ToImmutableArray();
			Metadata = metadata?.ToMetadataCollection() ?? MetadataCollection.Empty;
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="ChatCompletionResult"/> using specified parameters.
		/// </summary>
		/// <param name="client">The client that used to generate this completion.</param>
		/// <param name="model">The model descriptor that used to generate this completion.</param>
		/// <param name="choices">The list of available completion choices.</param>
		/// <param name="metadata">The collection of completion metadata, such as usage stats.</param>
		public ChatCompletionResult(
			LLMClient client,
			LLModelDescriptor model,
			IEnumerable<AssistantMessage> choices,
			IEnumerable<IMetadata>? metadata = null)
		{
			Client = client ?? throw new ArgumentNullException(nameof(client));
			Model = model ?? throw new ArgumentNullException(nameof(model));
			Choices = choices?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(choices));
			Metadata = metadata?.ToMetadataCollection() ?? MetadataCollection.Empty;

			if (Choices.Length == 0)
				throw new ArgumentException("Choices must have at least one element.", nameof(choices));
		}

		/// <summary>
		/// Creates a new instance of <see cref="ChatCompletionResult"/> using specified parameters.
		/// </summary>
		/// <param name="client">The client that used to generate this completion.</param>
		/// <param name="model">The model descriptor that used to generate this completion.</param>
		/// <param name="choices">The list of available message completion choices.</param>
		/// <param name="metadata">The collection of completion metadata, such as usage stats.</param>
		public ChatCompletionResult(
			LLMClient client,
			LLModelDescriptor model,
			ImmutableArray<AssistantMessage> choices,
			MetadataCollection? metadata = null)
		{
			Client = client ?? throw new ArgumentNullException(nameof(client));
			Model = model ?? throw new ArgumentNullException(nameof(model));
			Choices = choices;
			Metadata = metadata ?? MetadataCollection.Empty;

			if (Choices.Length == 0)
				throw new ArgumentException("Choices must have at least one element.", nameof(choices));
		}
	}
}