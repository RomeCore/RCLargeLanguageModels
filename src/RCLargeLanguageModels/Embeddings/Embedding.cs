using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using RCLargeLanguageModels.Metadata;

namespace RCLargeLanguageModels.Embeddings
{
	/// <summary>
	/// Represents a dense vector embedding of a text segment or token.
	/// </summary>
	public class Embedding : IEquatable<Embedding>, IReadOnlyList<float>
	{
		private const float Tolerance = 1e-8f;

		private readonly Lazy<float> _lengthLazy;
		private readonly Lazy<Embedding> _normalizedLazy;

		/// <summary>
		/// The embedding vector values as array.
		/// </summary>
		public ImmutableArray<float> Vector { get; }
		float IReadOnlyList<float>.this[int index] => Vector[index];

		/// <summary>
		/// The source model that embedding was created with. Can be <see langword="null"/> if unknown.
		/// </summary>
		public LLModelDescriptor SourceModel { get; }

		/// <summary>
		/// The count of dimensions of the embedding vector.
		/// </summary>
		public int Dimensions => Vector.Length;
		int IReadOnlyCollection<float>.Count => Dimensions;

		/// <summary>
		/// The size in bytes of the embedding vector.
		/// </summary>
		public int ByteSize => Vector.Length * sizeof(float);

		/// <summary>
		/// Gets the length of the embedding vector.
		/// </summary>
		public float Length => _lengthLazy.Value;

		/// <summary>
		/// Gets the value indicating whether vector is zero vector.
		/// </summary>
		public bool IsZeroVector => Length < Tolerance;

		/// <summary>
		/// Gets the value indicating whether the embedding vector is normalized.
		/// </summary>
		public bool IsNormalized => _lengthLazy.Value == 1.0f;

		/// <summary>
		/// Get the normalized version of embedding.
		/// </summary>
		public Embedding Normalized => _normalizedLazy.Value;

		private static Lazy<float> CreateLazyLength(ImmutableArray<float> vector)
		{
			return new Lazy<float>(() =>
			{
				double sum = 0;
				foreach (var value in vector)
				{
					sum += (double)value * value;
				}
				return (float)Math.Sqrt(sum);
			});
		}
		
		private static Lazy<Embedding> CreateLazyNormalized(Embedding embedding)
		{
			return new Lazy<Embedding>(() =>
			{
				if (embedding.IsNormalized)
					return embedding;

				float length = embedding.Length;
				if (length < Tolerance)
					return embedding; // Preserve zero vectors

				var vector = embedding.Vector;
				var builder = ImmutableArray.CreateBuilder<float>(vector.Length);
				for (int i = 0; i < vector.Length; i++)
				{
					builder.Add(vector[i] / length);
				}
				return new Embedding(builder.MoveToImmutable(), embedding.SourceModel);
			});
		}



		/// <summary>
		/// Creates a new instance of <see cref="Embedding"/> class using specified vector.
		/// </summary>
		/// <param name="vector">The embedding vector.</param>
		public Embedding(IEnumerable<float> vector)
		{
			Vector = vector.ToImmutableArray();
			SourceModel = null;

			_lengthLazy = CreateLazyLength(Vector);
			_normalizedLazy = CreateLazyNormalized(this);
		}

		/// <summary>
		/// Creates a new instance of <see cref="Embedding"/> class using specified vector and source model.
		/// </summary>
		/// <param name="vector">The embedding vector.</param>
		/// <param name="sourceModel">The source model that embedding was created with.</param>
		public Embedding(IEnumerable<float> vector, LLModelDescriptor sourceModel)
		{
			Vector = vector.ToImmutableArray();
			SourceModel = sourceModel;

			_lengthLazy = CreateLazyLength(Vector);
			_normalizedLazy = CreateLazyNormalized(this);
		}

		/// <summary>
		/// Creates a new instance of <see cref="Embedding"/> class using specified vector.
		/// </summary>
		/// <param name="vector">The embedding vector.</param>
		public Embedding(ImmutableArray<float> vector)
		{
			Vector = vector;
			SourceModel = null;

			_lengthLazy = CreateLazyLength(Vector);
			_normalizedLazy = CreateLazyNormalized(this);
		}

		/// <summary>
		/// Creates a new instance of <see cref="Embedding"/> class using specified vector and source model.
		/// </summary>
		/// <param name="vector">The embedding vector.</param>
		/// <param name="sourceModel">The source model that embedding was created with.</param>
		public Embedding(ImmutableArray<float> vector, LLModelDescriptor sourceModel)
		{
			Vector = vector;
			SourceModel = sourceModel;

			_lengthLazy = CreateLazyLength(Vector);
			_normalizedLazy = CreateLazyNormalized(this);
		}

		/// <summary>
		/// Checks if embeddings are fully compatible with each other.
		/// </summary>
		/// <param name="other">The other embedding to compare.</param>
		/// <returns><see langword="true"/> if other embedding has the same dimension count and same source model; otherwise, <see langword="false"/>.</returns>
		public bool IsFullyCompatibleWith(Embedding other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			return Dimensions == other.Dimensions && SourceModel == other.SourceModel;
		}

		/// <summary>
		/// Checks if embeddings are compatible with each other.
		/// </summary>
		/// <param name="other">The other embedding to compare.</param>
		/// <returns><see langword="true"/> if other embedding has the same dimension count; otherwise, <see langword="false"/>.</returns>
		public bool IsCompatibleWith(Embedding other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			return Dimensions == other.Dimensions;
		}



		public bool Equals(Embedding other)
		{
			if (other is null) return false;
			if (ReferenceEquals(this, other)) return true;
			if (Dimensions != other.Dimensions) return false;
			if (SourceModel != other.SourceModel) return false;
			return Vector.SequenceEqual(other.Vector) && SourceModel == other.SourceModel;
		}
		public override bool Equals(object obj) => Equals(obj as Embedding);

		public static bool operator == (Embedding left, Embedding right)
		{
			if (left is null && right is null)
				return true;
			if (left is null || right is null)
				return false;
			return left.Equals(right);
		}
		
		public static bool operator != (Embedding left, Embedding right)
		{
			return !(left == right);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return Vector.Aggregate(0, (acc, f) => (acc * 397) ^ f.GetHashCode())
					^ (SourceModel?.GetHashCode() ?? 0);
			}
		}

		public static implicit operator Embedding(ImmutableArray<float> vector) => new Embedding(vector);
		public static implicit operator Embedding(List<float> vector) => new Embedding(vector);
		public static implicit operator Embedding(float[] vector) => new Embedding(vector);
		public static implicit operator ImmutableArray<float>(Embedding embedding) => embedding.Vector;
		public static implicit operator ReadOnlySpan<float>(Embedding embedding) => embedding.Vector.AsSpan();
		public static implicit operator ReadOnlyMemory<float>(Embedding embedding) => embedding.Vector.AsMemory();



		public override string ToString()
		{
			string vectorString = string.Join(", ", Vector.Take(20));
			if (Vector.Length > 20)
				vectorString += "...";

			string sourceModelStr = string.Empty;
			if (SourceModel != null)
				sourceModelStr += ", " + SourceModel.DisplayName;

			return $"Embedding: Dimensions={Dimensions}, Vector=[{vectorString}]{sourceModelStr}";
		}

		/// <summary>
		/// Returns an enumerator that iterates through the embedding vector.
		/// </summary>
		/// <returns>An enumerator.</returns>
		public IEnumerator<float> GetEnumerator()
		{
			return ((IEnumerable<float>)Vector).GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}