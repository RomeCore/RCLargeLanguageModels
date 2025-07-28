using System;

namespace RCLargeLanguageModels.Statistics
{
	/// <summary>
	/// A static utility class for collecting and managing token usage statistics across users, clients, and models.
	/// </summary>
	public static class TokenUsageStatsCollector
	{
		/// <summary>
		/// Represents the local user identifier for usage tracking when no specific user is provided.
		/// </summary>
		public const string LocalUser = "LocalUser";

		/// <summary>
		/// Gets or sets the shared token usage stats collector.
		/// </summary>
		public static ITokenUsageStatsCollector Shared { get; set; }

		/// <summary>
		/// Appends the usage of tokens to the collector using the local user.
		/// </summary>
		/// <param name="clientName">The AI client provider name.</param>
		/// <param name="modelName">The AI model name.</param>
		/// <param name="inputTokens">The number of input tokens used; -1 if unknown.</param>
		/// <param name="outputTokens">The number of output tokens used; -1 if unknown.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="clientName"/> or <paramref name="modelName"/> is null.</exception>
		public static void AppendUsage(string clientName, string modelName, int inputTokens, int outputTokens)
		{
			AppendUsage(LocalUser, clientName, modelName, inputTokens, outputTokens);
		}

		/// <summary>
		/// Appends the usage of tokens to the collector for a specific user.
		/// </summary>
		/// <param name="userName">The user name associated with the usage.</param>
		/// <param name="clientName">The AI client provider name.</param>
		/// <param name="modelName">The AI model name.</param>
		/// <param name="inputTokens">The number of input tokens used; -1 if unknown.</param>
		/// <param name="outputTokens">The number of output tokens used; -1 if unknown.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="userName"/>, <paramref name="clientName"/>, or <paramref name="modelName"/> is null.</exception>
		public static void AppendUsage(string userName, string clientName, string modelName, int inputTokens, int outputTokens)
		{
			if (userName == null)
				throw new ArgumentNullException(nameof(userName));
			if (clientName == null)
				throw new ArgumentNullException(nameof(clientName));
			if (modelName == null)
				throw new ArgumentNullException(nameof(modelName));

			Shared?.AppendUsage(userName, clientName, modelName, inputTokens, outputTokens);
		}
	}
}