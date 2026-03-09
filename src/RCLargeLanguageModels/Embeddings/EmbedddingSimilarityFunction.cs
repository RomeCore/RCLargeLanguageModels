using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Embeddings
{
	/// <summary>
	/// Represents a function that calculates the similarity between two embeddings.
	/// </summary>
	/// <param name="a">First embedding.</param>
	/// <param name="b">Second embedding.</param>
	/// <returns>The similarity score.</returns>
	public delegate float EmbeddingSimilarityFunction(ReadOnlySpan<float> a, ReadOnlySpan<float> b);
}