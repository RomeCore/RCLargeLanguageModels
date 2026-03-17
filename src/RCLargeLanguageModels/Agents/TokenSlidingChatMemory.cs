using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Statistics;
using RCLargeLanguageModels.Tools;

namespace RCLargeLanguageModels.Agents
{
	/// <summary>
	/// Represents the LLM chat memory that simply appends messages and trims old messages while retrieving.
	/// </summary>
	public class TokenSlidingChatMemory : LLMChatMemory
	{
		private readonly AsyncCache<(IMessage, IEnumerable<ITool>), int> _tokenCountCache;

		/// <summary>
		/// Gets or sets the system instructions.
		/// </summary>
		public string SystemInstructions { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the list of non-system messages. Messages can be removed from this list if them will be trimmed.
		/// </summary>
		public List<IMessage> Messages { get; set; } = new();

		/// <summary>
		/// Gets or sets the token counter used for old message trimming.
		/// </summary>
		public ITokenCounter TokenCounter { get; set; } = Statistics.TokenCounter.Naive;

		/// <summary>
		/// Gets or sets the serialization schema used for serializing messages for token counting.
		/// </summary>
		public IMessageTokenSerializationSchema MessageTokenSerializationSchema { get; set; } = Statistics.MessageTokenSerializationSchema.Default;

		/// <summary>
		/// Gets or sets the maximum context window in tokens that target LLM model should support (default is 128000), this value will be multiplied by <see cref="Tolerance"/> and used for trimming.
		/// </summary>
		public int TargetTokens { get; set; } = 128000;

		/// <summary>
		/// Gets or sets the trim tokens tolerance multiplier, default is 0.7 or 70%.
		/// </summary>
		public float Tolerance { get; set; } = 0.7f;

		/// <summary>
		/// Initializes a new instance of <see cref="TokenSlidingChatMemory"/> class.
		/// </summary>
		public TokenSlidingChatMemory()
		{
			_tokenCountCache = new ((t, ct) =>
			{
				return TokenCounter.CountAsync(t.Item1, t.Item2, MessageTokenSerializationSchema, ct);
			}, slidingExpirationTime: TimeSpan.FromHours(1));
		}

		/// <summary>
		/// Initializes a new instance of <see cref="TokenSlidingChatMemory"/> class.
		/// </summary>
		/// <param name="tokenCountCacheExpirationTime">
		/// The time that token count cache will be expired and counted again.
		/// </param>
		public TokenSlidingChatMemory(TimeSpan tokenCountCacheExpirationTime)
		{
			_tokenCountCache = new((t, ct) =>
			{
				return TokenCounter.CountAsync(t.Item1, t.Item2, MessageTokenSerializationSchema, ct);
			}, slidingExpirationTime: tokenCountCacheExpirationTime);
		}

		public override async Task<IEnumerable<IMessage>> AppendAsync(IUserMessage userMessage,
			LLModel targetModel, CancellationToken cancellationToken = default)
		{
			return await AppendInternal(new IMessage[] { await TransformUserMessage(userMessage, cancellationToken) },
				targetModel, cancellationToken);
		}

		public override Task<IEnumerable<IMessage>> AppendAsync(IEnumerable<IMessage> previousMessages, IAssistantMessage assistantMessage,
			IEnumerable<IToolMessage> toolMessages, LLModel targetModel, CancellationToken cancellationToken = default)
		{
			return AppendInternal(new IMessage[] { assistantMessage }.Concat(toolMessages),
				targetModel, cancellationToken);
		}

		private async Task<IEnumerable<IMessage>> AppendInternal(IEnumerable<IMessage> messagesToAdd,
			LLModel targetModel, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(SystemInstructions))
				SystemInstructions = "You are a helpful assistant";
			Messages ??= new();
			TokenCounter ??= Statistics.TokenCounter.Naive;
			MessageTokenSerializationSchema ??= Statistics.MessageTokenSerializationSchema.Default;
			var maxTokens = (int)(TargetTokens * Tolerance);

			_tokenCountCache.RemoveExpiredItems();

			var systemMessage = new SystemMessage(SystemInstructions);
			List<IMessage> result = new()
			{
				systemMessage
			}, messagesToAddList = messagesToAdd.ToList();

			// Count tokens in system instructions + new messages
			// Then strictly check if token count exceeds maximum
			int currentTokenCount = await _tokenCountCache.GetAsync((systemMessage, targetModel.Tools),
				cancellationToken);
			foreach (var message in messagesToAddList)
			{
				currentTokenCount += await _tokenCountCache.GetAsync((message, targetModel.Tools),
					cancellationToken);
				if (currentTokenCount > maxTokens)
					throw new Exception("New messages along with system messages exceeds trim token count.");
				result.Add(message);
			}

			// Check + add messages in reverse order (most recent messages matters)
			for (int i = Messages.Count - 1; i >= 0; i--)
			{
				var message = Messages[i];
				int tokenCount = await _tokenCountCache.GetAsync((message, targetModel.Tools),
					cancellationToken);
				if (currentTokenCount + tokenCount > maxTokens)
				{
					Messages.RemoveRange(0, i + 1);
					break;
				}
				currentTokenCount += tokenCount;
				// Just after the system message (insert in reverse order again)
				// Order becames normal
				result.Insert(1, message);
			}

			// Remove headless tool messages (the tool messages without tool call before them)
			// The tool calls contined in previous assistant message
			while (result.Count > 1 && result[1] is IToolMessage)
				result.RemoveAt(1);

			// Append new messages to our instance's collection
			Messages.AddRange(messagesToAddList);

			return result;
		}
	}
}