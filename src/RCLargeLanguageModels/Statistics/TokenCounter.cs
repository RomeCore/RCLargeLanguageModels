using System.Threading;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Statistics
{
	/// <summary>
	/// Represents the token counter that counts number of tokens in the input text.
	/// </summary>
	public abstract class TokenCounter : ITokenCounter
	{
		public abstract Task<int> CountAsync(string text, CancellationToken cancellationToken = default);

		/// <summary>
		/// Gets the simplest, fastest, naive implementation of the token counter with 2.5 division coefficient.
		/// </summary>
		public static TokenCounter Naive { get; } = new NaiveTokenCounter(2.5f);
	}
}