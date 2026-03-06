using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using RCLargeLanguageModels.Metadata;

namespace RCLargeLanguageModels.Completions
{
	/// <summary>
	/// Represents a completed LLM completion result.
	/// </summary>
	public class CompletionResult : ICompletionResult
	{
		/// <inheritdoc/>
		public LLMClient Client { get; }

		/// <inheritdoc/>
		public LLModelDescriptor Model { get; }

		/// <inheritdoc cref="IGenerationResult{T}.Choices"/>
		public ImmutableArray<Completion> Choices { get; }
		IReadOnlyList<ICompletion> IGenerationResult<ICompletion>.Choices => Choices;

		/// <inheritdoc cref="IGenerationResult{T}.Completion"/>
		public Completion Completion => Choices[0];
		ICompletion IGenerationResult<ICompletion>.Completion => Completion;

		/// <inheritdoc/>
		public string? Content => Completion.Content;

		/// <inheritdoc cref="IGenerationResult{T}.Metadata"/>
		public MetadataCollection Metadata { get; }
		IMetadataCollection IMetadataProvider.Metadata => Metadata;
		IMetadataCollection IGenerationResult<ICompletion>.Metadata => Metadata;

		/// <inheritdoc/>
		public IUsageMetadata? UsageMetadata => Metadata.TryGet<IUsageMetadata>();

		/// <summary>
		/// Creates a new instance of <see cref="CompletionResult"/> using specified parameters.
		/// </summary>
		/// <param name="client">The client that used to generate this completion.</param>
		/// <param name="model">The model descriptor that used to generate this completion.</param>
		/// <param name="choice">The single completion choice.</param>
		/// <param name="metadata">The collection of completion metadata, such as usage stats.</param>
		public CompletionResult(
			LLMClient client,
			LLModelDescriptor model,
			Completion choice,
			IEnumerable<IMetadata>? metadata = null)
		{
			if (choice == null)
				throw new ArgumentNullException(nameof(choice));

			Client = client ?? throw new ArgumentNullException(nameof(client));
			Model = model ?? throw new ArgumentNullException(nameof(model));
			Choices = new Completion[] { choice }.ToImmutableArray();
			Metadata = metadata?.ToMetadataCollection() ?? MetadataCollection.Empty;
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="CompletionResult"/> using specified parameters.
		/// </summary>
		/// <param name="client">The client that used to generate this completion.</param>
		/// <param name="model">The model descriptor that used to generate this completion.</param>
		/// <param name="choices">The list of available completion choices.</param>
		/// <param name="metadata">The collection of completion metadata, such as usage stats.</param>
		public CompletionResult(
			LLMClient client,
			LLModelDescriptor model,
			IEnumerable<Completion> choices,
			IEnumerable<IMetadata>? metadata = null)
		{
			Client = client ?? throw new ArgumentNullException(nameof(client));
			Model = model ?? throw new ArgumentNullException(nameof(model));
			Choices = choices?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(choices));
			Metadata = metadata?.ToMetadataCollection() ?? MetadataCollection.Empty;

			if (Choices.Length == 0)
				throw new ArgumentException("Choices must have at least one element.", nameof(choices));
			if (Choices.Any(c => c == null))
				throw new ArgumentNullException(nameof(choices), "One element in choices is null.");
		}

		/// <summary>
		/// Creates a new instance of <see cref="CompletionResult"/> using specified parameters.
		/// </summary>
		/// <param name="client">The client that used to generate this completion.</param>
		/// <param name="model">The model descriptor that used to generate this completion.</param>
		/// <param name="choices">The list of available completion choices.</param>
		/// <param name="metadata">The collection of completion metadata, such as usage stats.</param>
		public CompletionResult(
			LLMClient client,
			LLModelDescriptor model,
			ImmutableArray<Completion> choices,
			MetadataCollection? metadata = null)
		{
			Client = client ?? throw new ArgumentNullException(nameof(client));
			Model = model ?? throw new ArgumentNullException(nameof(model));
			Choices = choices;
			Metadata = metadata ?? MetadataCollection.Empty;

			if (Choices.Length == 0)
				throw new ArgumentException("Choices must have at least one element.", nameof(choices));
			if (Choices.Any(c => c == null))
				throw new ArgumentNullException(nameof(choices), "One element in choices is null.");
		}
	}
}