using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Metadata
{
	/// <summary>
	/// Represents an usage completion metadata that contains input/output tokens that associated with completion.
	/// </summary>
	public interface IUsageMetadata : ICompletionMetadata
	{
		/// <summary>
		/// Gets the input (prompt) tokens.
		/// </summary>
		public int InputTokens { get; }

		/// <summary>
		/// Gets the output (generated) tokens.
		/// </summary>
		public int OutputTokens { get; }

		/// <summary>
		/// Gets the combined input + output tokens;
		/// </summary>
		public int TotalTokens { get; }
	}

	/// <summary>
	/// Represents an usage completion metadata that contains input (cache hit/miss)/output/cached tokens that associated with completion.
	/// </summary>
	public interface IUsageCacheMetadata : IUsageMetadata
	{
		/// <summary>
		/// Gets the input tokens that had cache hit and costs less.
		/// </summary>
		public int InputCacheHitTokens { get; }

		/// <summary>
		/// Gets the input tokens that had cache miss and costs normally.
		/// </summary>
		public int InputCacheMissTokens { get; }

		/// <summary>
		/// Gets the count of cached tokens that may affect the cache hits in further completions.
		/// </summary>
		public int CachedTokens { get; }
	}

	/// <inheritdoc cref="IUsageMetadata"/>
	public class UsageMetadata : IUsageMetadata
	{
		public int InputTokens { get; }
		public int OutputTokens { get; }
		public int TotalTokens => InputTokens + OutputTokens;

		/// <summary>
		/// Creates a new instance of <see cref="UsageMetadata"/>.
		/// </summary>
		public UsageMetadata(int inputTokens, int outputTokens)
		{
			InputTokens = inputTokens;
			OutputTokens = outputTokens;
		}
	}

	/// <inheritdoc cref="IUsageCacheMetadata"/>
	public class UsageCacheMetadata : UsageMetadata, IUsageCacheMetadata
	{
		public int InputCacheHitTokens { get; }
		public int InputCacheMissTokens { get; }
		public int CachedTokens { get; }

		/// <summary>
		/// Creates a new instance of <see cref="UsageCacheMetadata"/>.
		/// </summary>
		public UsageCacheMetadata(int inputCacheHitTokens, int inputCacheMissTokens, int outputTokens, int cachedTokens)
			: base(inputCacheHitTokens + inputCacheMissTokens, outputTokens)
		{
			InputCacheHitTokens = inputCacheHitTokens;
			InputCacheMissTokens = inputCacheMissTokens;
			CachedTokens = cachedTokens;
		}
	}
}