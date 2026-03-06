using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Metadata;

namespace RCLargeLanguageModels.Embeddings
{
	/// <summary>
	/// Represents an embedding result containing one or more embeddings.
	/// </summary>
	public class EmbeddingResult : IEmbeddingResult
	{
		/// <inheritdoc/>
		public LLMClient Client { get; }

		/// <inheritdoc/>
		public LLModelDescriptor Model { get; }

		/// <summary>
		/// Gets the list of embeddings.
		/// </summary>
		public ImmutableArray<Embedding> Embeddings { get; }
		IReadOnlyList<Embedding> IGenerationResult<Embedding>.Choices => Embeddings;

		/// <summary>
		/// Gets the first embedding (convenience property for single embedding results).
		/// </summary>
		public Embedding Embedding => Embeddings[0];
		Embedding IGenerationResult<Embedding>.Completion => Embedding;

		/// <inheritdoc cref="IGenerationResult{T}.Metadata"/>
		public MetadataCollection Metadata { get; }
		IMetadataCollection IMetadataProvider.Metadata => Metadata;
		IMetadataCollection IGenerationResult<Embedding>.Metadata => Metadata;

		/// <inheritdoc/>
		public IUsageMetadata UsageMetadata => Metadata?.TryGet<IUsageMetadata>();

		/// <summary>
		/// Gets the count of embeddings.
		/// </summary>
		public int Count => Embeddings.Length;

		/// <summary>
		/// Gets the embedding at the specified index.
		/// </summary>
		/// <param name="index">The index of the embedding to get.</param>
		/// <returns>The embedding at the specified index.</returns>
		public Embedding this[int index] => Embeddings[index];

		/// <summary>
		/// Creates a new instance of <see cref="EmbeddingResult"/> using specified parameters.
		/// </summary>
		/// <param name="client">The client that generated this embedding.</param>
		/// <param name="model">The model descriptor used to generate this embedding.</param>
		/// <param name="embedding">The single embedding.</param>
		/// <param name="metadata">The collection of embedding metadata, such as usage stats.</param>
		public EmbeddingResult(
			LLMClient client,
			LLModelDescriptor model,
			Embedding embedding,
			IEnumerable<IMetadata>? metadata = null)
		{
			if (embedding == null)
				throw new ArgumentNullException(nameof(embedding));

			Client = client ?? throw new ArgumentNullException(nameof(client));
			Model = model ?? throw new ArgumentNullException(nameof(model));
			Embeddings = new Embedding[] { embedding }.ToImmutableArray();
			Metadata = metadata?.ToMetadataCollection() ?? MetadataCollection.Empty;
		}

		/// <summary>
		/// Creates a new instance of <see cref="EmbeddingResult"/> using specified parameters.
		/// </summary>
		/// <param name="client">The client that generated these embeddings.</param>
		/// <param name="model">The model descriptor used to generate these embeddings.</param>
		/// <param name="embeddings">The list of embeddings.</param>
		/// <param name="metadata">The collection of embedding metadata, such as usage stats.</param>
		public EmbeddingResult(
			LLMClient client,
			LLModelDescriptor model,
			IEnumerable<Embedding> embeddings,
			IEnumerable<IMetadata>? metadata = null)
		{
			Client = client ?? throw new ArgumentNullException(nameof(client));
			Model = model ?? throw new ArgumentNullException(nameof(model));
			Embeddings = embeddings?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(embeddings));
			Metadata = metadata?.ToMetadataCollection() ?? MetadataCollection.Empty;

			if (Embeddings.Length == 0)
				throw new ArgumentException("Embeddings must have at least one element.", nameof(embeddings));
		}

		/// <summary>
		/// Creates a new instance of <see cref="EmbeddingResult"/> using specified parameters.
		/// </summary>
		/// <param name="client">The client that generated these embeddings.</param>
		/// <param name="model">The model descriptor used to generate these embeddings.</param>
		/// <param name="embeddings">The immutable array of embeddings.</param>
		/// <param name="metadata">The collection of embedding metadata, such as usage stats.</param>
		public EmbeddingResult(
			LLMClient client,
			LLModelDescriptor model,
			ImmutableArray<Embedding> embeddings,
			MetadataCollection? metadata = null)
		{
			Client = client ?? throw new ArgumentNullException(nameof(client));
			Model = model ?? throw new ArgumentNullException(nameof(model));
			Embeddings = embeddings;
			Metadata = metadata ?? MetadataCollection.Empty;

			if (Embeddings.Length == 0)
				throw new ArgumentException("Embeddings must have at least one element.", nameof(embeddings));
		}
	}
}