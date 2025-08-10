using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RCLargeLanguageModels
{
	/// <summary>
	/// Utility class for enumerable operations.
	/// </summary>
	public static class EnumerableUtils
	{
		/// <summary>
		/// The wrapper around a params array to make it enumerable.
		/// </summary>
		/// <typeparam name="T">The type of elements in the sequence.</typeparam>
		/// <param name="items">The params array.</param>
		/// <returns>A sequence of elements from the provided params array.</returns>
		public static IEnumerable<T> Params<T>(params T[] items)
		{
			return items;
		}
		
		/// <summary>
		/// Returns a sequence of non-null elements from the provided params array.
		/// </summary>
		/// <typeparam name="T">The type of elements in the sequence.</typeparam>
		/// <param name="items">The params array to filter.</param>
		/// <returns>A sequence of non-null elements from the provided params array.</returns>
		public static IEnumerable<T> NotNull<T>(params T?[] items)
		{
			return items.Where(item => item != null);
		}

		/// <summary>
		/// Returns a sequence of non-null elements from the provided params array.
		/// </summary>
		/// <typeparam name="T">The type of elements in the sequence.</typeparam>
		/// <param name="items">The params array to filter.</param>
		/// <returns>A sequence of non-null elements from the provided params array.</returns>
		public static IEnumerable<T> NotNull<T>(params T?[] items)
			where T : struct
		{
			return items.Where(item => item.HasValue).Select(item => item.Value);
		}
	}
}