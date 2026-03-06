using System;
using System.Text.Json.Serialization;

namespace RCLargeLanguageModels.Statistics
{
	/// <summary>
	/// Represents mutable token usage statistics for an AI model.
	/// </summary>
	public class TokenUsageStats
	{
		/// <summary>
		/// Gets or sets the number of input tokens used. A value of -1 indicates unknown usage.
		/// </summary>
		[JsonPropertyName("inputTokens")]
		public int InputTokens { get; set; }

		/// <summary>
		/// Gets or sets the number of output tokens used. A value of -1 indicates unknown usage.
		/// </summary>
		[JsonPropertyName("outputTokens")]
		public int OutputTokens { get; set; }

		/// <summary>
		/// Gets the total number of tokens (input + output). Returns -1 if either <see cref="InputTokens"/> or <see cref="OutputTokens"/> is -1.
		/// </summary>
		[JsonIgnore]
		public int TotalTokens => (InputTokens == -1 || OutputTokens == -1) ? -1 : InputTokens + OutputTokens;

		/// <summary>
		/// Gets or sets the number of times this model has been used.
		/// </summary>
		[JsonPropertyName("usageCount")]
		public int UsageCount { get; set; }

		/// <summary>
		/// Gets a value indicating whether the input token count is unknown (-1).
		/// </summary>
		[JsonIgnore]
		public bool IsInputUnknown => InputTokens == -1;

		/// <summary>
		/// Gets a value indicating whether the output token count is unknown (-1).
		/// </summary>
		[JsonIgnore]
		public bool IsOutputUnknown => OutputTokens == -1;
	}

	/// <summary>
	/// Represents an immutable view of token usage statistics for an AI model.
	/// </summary>
	public class ReadonlyTokenUsageStats
	{
		private readonly TokenUsageStats _tokenUsage;

		/// <summary>
		/// Gets the number of input tokens used. A value of -1 indicates unknown usage.
		/// </summary>
		public int InputTokens => _tokenUsage.InputTokens;

		/// <summary>
		/// Gets the number of output tokens used. A value of -1 indicates unknown usage.
		/// </summary>
		public int OutputTokens => _tokenUsage.OutputTokens;

		/// <summary>
		/// Gets the total number of tokens (input + output). Returns -1 if either <see cref="InputTokens"/> or <see cref="OutputTokens"/> is -1.
		/// </summary>
		public int TotalTokens => _tokenUsage.TotalTokens;

		/// <summary>
		/// Gets the number of times this model has been used.
		/// </summary>
		public int UsageCount => _tokenUsage.UsageCount;

		/// <summary>
		/// Gets a value indicating whether the input token count is unknown (-1).
		/// </summary>
		public bool IsInputUnknown => _tokenUsage.IsInputUnknown;

		/// <summary>
		/// Gets a value indicating whether the output token count is unknown (-1).
		/// </summary>
		public bool IsOutputUnknown => _tokenUsage.IsOutputUnknown;

		/// <summary>
		/// Initializes a new instance of <see cref="ReadonlyTokenUsageStats"/> with the specified token usage data.
		/// </summary>
		/// <param name="tokenUsage">The token usage data to wrap.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="tokenUsage"/> is null.</exception>
		public ReadonlyTokenUsageStats(TokenUsageStats tokenUsage)
		{
			_tokenUsage = tokenUsage ?? throw new ArgumentNullException(nameof(tokenUsage));
		}
	}
}