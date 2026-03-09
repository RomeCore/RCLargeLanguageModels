using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace RCLargeLanguageModels.Embeddings
{
	/// <summary>
	/// The class that provides extension methods for embedding vectors.
	/// </summary>
	public static class EmbeddingExtensions
	{
		/// <summary>
		/// Converts the embedding vector to a byte array.
		/// </summary>
		/// <param name="embedding">The embedding vector.</param>
		/// <returns>A byte array containing the embedding vector float values.</returns>
		public static byte[] ToByteArray(this Embedding embedding)
		{
			return MemoryMarshal.AsBytes(embedding.Vector.AsSpan()).ToArray();
		}
	}
}