using System;
using System.Text.Json;
using RCLargeLanguageModels.Agents;

namespace RCLargeLanguageModels.Embeddings.Database
{
	/// <summary>
	/// Represents properties of a semantic sector.
	/// </summary>
	/// <typeparam name="T">The type of data stored in the sector.</typeparam>
	public class SemanticSectorProperties<T>
	{
		/// <summary>
		/// Gets or sets the batch size for embedding operations.
		/// </summary>
		public int EmbeddingBatchSize { get; set; } = 16;

		/// <summary>
		/// Gets or sets a function that retrieves input data from data to store.
		/// </summary>
		public Func<T, string> InputGetter { get; set; } = data => data.ToString();

		/// <summary>
		/// Gets or sets a transformer that converts data into a format suitable for semantic search and analysis.
		/// </summary>
		public ISemanticTransformer<string, string> InputTransformer { get; set; } = SemanticTransformer<string, string>.PassThrough;

		/// <summary>
		/// Gets or sets the serialization options used for serializing and deserializing objects.
		/// </summary>
		public JsonSerializerOptions SerializationOptions { get; set; } = new JsonSerializerOptions();

		/// <summary>
		/// Gets or sets a function that calculates the similarity between two embeddings.
		/// </summary>
		public EmbeddingSimilarityFunction SimilarityFunction { get; set; } = EmbeddingMetrics.DotProduct;
	}
}