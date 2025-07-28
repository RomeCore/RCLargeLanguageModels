using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Prompting
{
	/// <summary>
	/// Represents the token usage of a prompt, example, or LLM evaluation algorithm (code completion or fetching file info).
	/// </summary>
	public enum TokenUsage
	{
		/// <summary>
		/// The lowest token usage; prompts are simple, and LLM evaluations are lightweight, requiring minimal tokens.
		/// </summary>
		Low = 1,

		/// <summary>
		/// Moderate token usage; prompts and evaluations require a moderate number of tokens.
		/// </summary>
		Medium = 3,

		/// <summary>
		/// The highest token usage; prompts and evaluations are maximally complex, requiring the most tokens.
		/// </summary>
		High = 5
	}

	public static class TokenUsageExtensions
	{
		/// <summary>
		/// Throws an <see cref="ArgumentOutOfRangeException"/> if the given token usage is out of range.
		/// </summary>
		/// <param name="tokenUsage">The token usage value that must be in range of [1, 5]</param>
		/// <exception cref="ArgumentOutOfRangeException"/>
		public static void ThrowIfOutOfRange(this TokenUsage tokenUsage)
		{
			if (tokenUsage < TokenUsage.Low || tokenUsage > TokenUsage.High)
				throw new ArgumentOutOfRangeException(nameof(tokenUsage), "TokenUsage must be between Low and High.");
		}

		// TODO: Remove or something else
		/*/// <summary>
		/// Gets the most suitable token usage from the given map based on the target token usage.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="tokenUsageMap">The dictionary to select from.</param>
		/// <param name="target">The target token usage.</param>
		/// <returns>The value that has the nearest key to <paramref name="target"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="tokenUsageMap"/> is <see langword="null"/></exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="target"/> is out of range of [1, 5]</exception>
		public static T GetMostSuitableByTokenUsage<T>(this IDictionary<TokenUsage, T> tokenUsageMap, TokenUsage target)
		{
			if (tokenUsageMap == null)
				throw new ArgumentNullException(nameof(tokenUsageMap));
			target.ThrowIfOutOfRange();

			if (tokenUsageMap.Count == 0)
				return default;

			return tokenUsageMap.MinBy(kv => Math.Abs((int)kv.Key - (int)target)).Value;
		}*/

		/// <summary>
		/// Gets the most suitable item from an <see cref="IEnumerable{T}"/> based on token usage.
		/// </summary>
		/// <typeparam name="T">The type of the items in the collection.</typeparam>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="items">The collection of items to select from.</param>
		/// <param name="tokenUsageSelector">A function to extract the token usage from an item.</param>
		/// <param name="resultSelector">A function to project the result from the selected item.</param>
		/// <param name="targetTokenUsage">The target token usage.</param>
		/// <returns>The item with the nearest token usage to the target.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="items"/>, <paramref name="tokenUsageSelector"/>, or <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="targetTokenUsage"/> is out of range of [1, 5].</exception>
		public static TResult GetMostSuitableByTokenUsage<T, TResult>(
			this IEnumerable<T> items,
			Func<T, TokenUsage> tokenUsageSelector,
			Func<T, TResult> resultSelector,
			TokenUsage targetTokenUsage)
		{
			if (items == null)
				throw new ArgumentNullException(nameof(items));
			if (tokenUsageSelector == null)
				throw new ArgumentNullException(nameof(tokenUsageSelector));
			if (resultSelector == null)
				throw new ArgumentNullException(nameof(resultSelector));
			targetTokenUsage.ThrowIfOutOfRange();

			return items
				.Select(item => new { Item = item, TokenUsage = tokenUsageSelector(item) })
				.OrderBy(x => Math.Abs((int)x.TokenUsage - (int)targetTokenUsage))
				.Select(x => resultSelector(x.Item))
				.FirstOrDefault();
		}
	}
}