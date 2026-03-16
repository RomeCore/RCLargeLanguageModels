using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Statistics
{
	/// <summary>
	/// Represents the token counter that counts number of tokens in the input text.
	/// </summary>
	public interface ITokenCounter
	{
		/// <summary>
		/// Counts tokens in the input text.
		/// </summary>
		/// <param name="text">The text to count tokens for.</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>The count of tokens.</returns>
		Task<int> CountAsync(string text, CancellationToken cancellationToken = default);
	}
}