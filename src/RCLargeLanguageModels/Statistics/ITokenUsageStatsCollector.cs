using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Statistics
{
	/// <summary>
	/// Represents the collector of token usage statistics.
	/// </summary>
	public interface ITokenUsageStatsCollector
	{
		/// <summary>
		/// Appends the usage of tokens to the collector for a specific user.
		/// </summary>
		/// <param name="userName">The user name associated with the usage.</param>
		/// <param name="clientName">The AI client provider name.</param>
		/// <param name="modelName">The AI model name.</param>
		/// <param name="inputTokens">The number of input tokens used; -1 if unknown.</param>
		/// <param name="outputTokens">The number of output tokens used; -1 if unknown.</param>
		void AppendUsage(string userName, string clientName, string modelName, int inputTokens, int outputTokens);
	}
}