using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Embeddings
{
	/// <summary>
	/// Provides common metrics for comparing embedding vectors.
	/// </summary>
	public static class EmbeddingMetrics
	{
		private const float Tolerance = 1e-8f;

		/// <summary>
		/// Computes cosine similarity between two embeddings.
		/// </summary>
		/// <param name="a">First embedding.</param>
		/// <param name="b">Second embedding.</param>
		/// <returns>
		/// Similarity score between -1.0 (completely dissimilar) and 1.0 (identical direction).
		/// </returns>
		public static float CosineSimilarity(Embedding a, Embedding b)
		{
			ValidateCompatibility(a, b);

			float dot = DotProduct(a, b);
			float magnitudeProduct = a.Length * b.Length;

			// Handle zero-vector edge cases
			if (magnitudeProduct < Tolerance)
				return 0f;

			return dot / magnitudeProduct;
		}

		/// <summary>
		/// Computes the dot product (scalar product) between two embeddings.
		/// </summary>
		public static float DotProduct(Embedding a, Embedding b)
		{
			ValidateCompatibility(a, b);

			float result = 0f;
			for (int i = 0; i < a.Dimensions; i++)
			{
				result += a.Vector[i] * b.Vector[i];
			}
			return result;
		}

		/// <summary>
		/// Computes Euclidean distance between two embeddings.
		/// </summary>
		public static float EuclideanDistance(Embedding a, Embedding b)
		{
			ValidateCompatibility(a, b);

			float sum = 0f;
			for (int i = 0; i < a.Dimensions; i++)
			{
				float diff = a.Vector[i] - b.Vector[i];
				sum += diff * diff;
			}
			return (float)Math.Sqrt(sum);
		}

		/// <summary>
		/// Computes Manhattan distance (L1 distance) between two embeddings.
		/// </summary>
		public static float ManhattanDistance(Embedding a, Embedding b)
		{
			ValidateCompatibility(a, b);

			float sum = 0f;
			for (int i = 0; i < a.Dimensions; i++)
			{
				sum += Math.Abs(a.Vector[i] - b.Vector[i]);
			}
			return sum;
		}

		/// <summary>
		/// Computes cosine similarity for pre-normalized embeddings.
		/// </summary>
		/// <remarks>
		/// 10-15x faster than regular <see cref="CosineSimilarity"/>. Use only when both embeddings are confirmed to be normalized (<see cref="Embedding.IsNormalized"/> == true).
		/// </remarks>
		public static float CosineSimilarityNormalized(Embedding a, Embedding b)
		{
			ValidateCompatibility(a, b);

			if (!a.IsNormalized || !b.IsNormalized)
			{
				throw new ArgumentException(
					"Both embeddings must be normalized. Check IsNormalized property first.");
			}

			return DotProduct(a, b);
		}

		private static void ValidateCompatibility(Embedding a, Embedding b)
		{
			if (a is null)
				throw new ArgumentNullException(nameof(a));
			if (b is null)
				throw new ArgumentNullException(nameof(b));

			if (a.Dimensions != b.Dimensions)
			{
				throw new ArgumentException(
					$"Embedding dimension mismatch: {a.Dimensions} vs {b.Dimensions}");
			}
		}
	}
}