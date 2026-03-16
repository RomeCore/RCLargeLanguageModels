using System.Threading;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Statistics
{
	/// <summary>
	/// The naive implementation of the <see cref="TokenCounter"/> that simply divides input text length by a specific amount.
	/// </summary>
	public class NaiveTokenCounter : TokenCounter
	{
		/// <summary>
		/// Gets the divider of input text length.
		/// </summary>
		public float Divider { get; }

		public NaiveTokenCounter(float divider = 2.5f)
		{
			Divider = divider;
		}

		public override Task<int> CountAsync(string text, CancellationToken cancellationToken = default)
		{
			return Task.FromResult((int)(text.Length / Divider));
		}
	}
}