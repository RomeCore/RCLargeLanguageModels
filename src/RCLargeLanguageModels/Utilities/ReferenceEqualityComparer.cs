using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RCLargeLanguageModels.Utilities
{
	/// <summary>
	/// A simple implementation of <see cref="IEqualityComparer{T}"/> that uses reference equality for comparison.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
	{
		/// <summary>
		/// Singleton instance of <see cref="ReferenceEqualityComparer{T}"/>.
		/// </summary>
		public static readonly ReferenceEqualityComparer<T> Instance = new ReferenceEqualityComparer<T>();

		private ReferenceEqualityComparer() { }

		public bool Equals(T x, T y)
		{
			return ReferenceEquals(x, y);
		}

		public int GetHashCode(T obj)
		{
			return RuntimeHelpers.GetHashCode(obj);
		}
	}
}