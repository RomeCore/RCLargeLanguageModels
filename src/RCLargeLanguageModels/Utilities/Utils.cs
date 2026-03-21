using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Utilities
{
	public static class Utils
	{
		/// <summary>
		/// Combines multiple collections or elements into one collection
		/// </summary>
		/// <typeparam name="T">The target type</typeparam>
		/// <param name="elementsOrCollections">Elements containing <see cref="IEnumerable{T}"/> or <typeparamref name="T"/></param>
		/// <returns>The combined collection</returns>
		/// <exception cref="ArgumentException">Thrown if one of elements isn't <see cref="IEnumerable{T}"/> or <typeparamref name="T"/></exception>
		public static IEnumerable<T> Combine<T>(params object[] elementsOrCollections)
		{
			foreach (var elementOrCollection in elementsOrCollections)
			{
				if (elementOrCollection is T element)
				{
					yield return element;
					continue;
				}
				if (elementOrCollection is IEnumerable<T> collection)
				{
					foreach (var item in collection)
						yield return item;
					continue;
				}

				throw new ArgumentException($"Invalid type: {elementOrCollection.GetType()}. " +
					$"Expected {typeof(T)} or {typeof(IEnumerable<T>)}.");

			}
		}

		/// <summary>
		/// Combines multiple collections or elements into one collection
		/// </summary>
		/// <param name="elementsOrCollections">Elements containing <see cref="IEnumerable"/> or <see cref="object"/></param>
		/// <returns>The combined collection</returns>
		public static IEnumerable Combine(params object[] elementsOrCollections)
		{
			foreach (var elementOrCollection in elementsOrCollections)
			{
				if (elementOrCollection is IEnumerable collection)
				{
					foreach (var item in collection)
						yield return item;
					continue;
				}
				yield return elementOrCollection;
			}
		}

		public static IEnumerable<T> Single<T>(T element)
		{
			yield return element;
		}

		public static IEnumerable Single(object element)
		{
			yield return element;
		}

		public static void Clamp(ref int value, int min, int max)
		{
			if (value < min)
				value = min;
			else if (value > max)
				value = max;
		}

		public static int Clamp(int value, int min, int max)
		{
			if (value < min)
				return min;
			else if (value > max)
				return max;
			return value;
		}
	}
}