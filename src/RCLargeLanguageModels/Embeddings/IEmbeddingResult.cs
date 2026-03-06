using RCLargeLanguageModels.Completions;

namespace RCLargeLanguageModels.Embeddings
{
	/// <summary>
	/// Represents a result of embedding generation.
	/// </summary>
	public interface IEmbeddingResult : IGenerationResult<Embedding>
	{
	}
}