using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Statistics;
using RCLargeLanguageModels.Tools;

namespace RCLargeLanguageModels.Agents
{
	/// <summary>
	/// The LLM chat memory that compresses context by summarizing old messages.
	/// </summary>
	public class SummarizingChatMemory : LLMChatMemory
	{
		private readonly AsyncCache<(IMessage, IEnumerable<ITool>), int> _tokenCountCache;

		public static LLMAgent CreateSummarizer(ILLMProvider llm)
		{
			return new StatelessAgent
			{
				SystemInstructions = "You are a conversation summarizer. " +
				"Your task is to summarize input LLM conversation history (including a previous summary, if provided) and return a next summary.",
				LLMProvider = llm
			};
		}

		/// <summary>
		/// Gets or sets the system instructions.
		/// </summary>
		public string SystemInstructions { get; set; } = "You are a helpful assistant.";

		/// <summary>
		/// Gets or sets the latest summary of removed messages and previous summary.
		/// </summary>
		public string? LatestSummary { get; set; }

		/// <summary>
		/// Gets or sets the list of non-system messages. Messages can be removed from this list if them will be summarized.
		/// </summary>
		public List<IMessage> Messages { get; set; } = new();

		/// <summary>
		/// Gets or sets the summarizer agent that will be used for summarization.
		/// </summary>
		public LLMAgent? Summarizer { get; set; }

		/// <summary>
		/// Gets or sets the token counter used for summarization triggering.
		/// </summary>
		public ITokenCounter TokenCounter { get; set; } = Statistics.TokenCounter.Naive;

		/// <summary>
		/// Gets or sets the serialization schema used for serializing messages for token counting and summary generation.
		/// </summary>
		public IMessageTokenSerializationSchema MessageSerializationSchema { get; set; } = Statistics.MessageTokenSerializationSchema.Default;

		/// <summary>
		/// Gets or sets the maximum context window in tokens that target LLM model should support (default is 128000),
		/// this value will be multiplied by <see cref="Tolerance"/> and used for summarization trigger.
		/// </summary>
		public int TargetTokens { get; set; } = 128000;

		/// <summary>
		/// Gets or sets the summarization trigger tokens tolerance multiplier, default is 0.7 or 70%.
		/// </summary>
		public float Tolerance { get; set; } = 0.7f;

		/// <summary>
		/// Gets or sets the count of last messages that will not be included in summarization sequence.
		/// </summary>
		public int KeepLastNMessages { get; set; } = 0;

		/// <summary>
		/// Initializes a new instance of <see cref="TokenSlidingChatMemory"/> class.
		/// </summary>
		public SummarizingChatMemory()
		{
			_tokenCountCache = new((t, ct) =>
			{
				return TokenCounter.CountAsync(t.Item1, t.Item2, MessageSerializationSchema, ct);
			}, slidingExpirationTime: TimeSpan.FromHours(1));
		}

		/// <summary>
		/// Initializes a new instance of <see cref="TokenSlidingChatMemory"/> class.
		/// </summary>
		/// <param name="tokenCountCacheExpirationTime">
		/// The time that token count cache will be expired and counted again.
		/// </param>
		public SummarizingChatMemory(TimeSpan tokenCountCacheExpirationTime)
		{
			_tokenCountCache = new((t, ct) =>
			{
				return TokenCounter.CountAsync(t.Item1, t.Item2, MessageSerializationSchema, ct);
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

		private ISystemMessage BuildSystemMessage()
		{
			string sysMsgContent = string.IsNullOrWhiteSpace(LatestSummary) ?
				SystemInstructions :
				$"{SystemInstructions}\n\nLast conversation summary: {LatestSummary}";
			return new SystemMessage(sysMsgContent);
		}

		private async Task<IEnumerable<IMessage>> AppendInternal(
			IEnumerable<IMessage> messagesToAdd,
			LLModel targetModel,
			CancellationToken cancellationToken)
		{
			if (Summarizer == null)
				throw new InvalidOperationException("Summarizer is not set.");

			if (string.IsNullOrWhiteSpace(SystemInstructions))
				SystemInstructions = "You are a helpful assistant";
			Messages ??= new();
			TokenCounter ??= Statistics.TokenCounter.Naive;
			MessageSerializationSchema ??= Statistics.MessageTokenSerializationSchema.Default;
			int maxTokens = (int)(TargetTokens * Tolerance);

			_tokenCountCache.RemoveExpiredItems();

			List<IMessage> result = new(), messagesToAddList = messagesToAdd.ToList();

			int totalTokens = 0;
			var systemMessage = BuildSystemMessage();
			foreach (var msg in new IMessage[] { systemMessage }.Concat(Messages).Concat(messagesToAddList))
				totalTokens += await _tokenCountCache.GetAsync((msg, targetModel.Tools), cancellationToken);

			while (totalTokens > maxTokens)
			{
				if (Messages.Count <= KeepLastNMessages + 1)
					break;

				List<IMessage> messagesToSummarize = new();
				StringBuilder summaryInput = new();

				int accumulatedTokens = 0;
				if (!string.IsNullOrWhiteSpace(LatestSummary))
				{
					summaryInput.Append("Previous messages summary: ").AppendLine(LatestSummary).AppendLine();
					accumulatedTokens = await TokenCounter.CountAsync(LatestSummary, cancellationToken);
				}

				foreach (var msg in Messages)
				{
					int currentTokens = await _tokenCountCache.GetAsync((msg, targetModel.Tools), cancellationToken);
					if (accumulatedTokens + currentTokens > maxTokens)
					{
						break;
					}
					messagesToSummarize.Add(msg);
					accumulatedTokens += currentTokens;
				}

				summaryInput.AppendLine("Messages:");
				foreach (var msg in messagesToSummarize)
				{
					var serialized = MessageSerializationSchema
						.SerializeMessage(msg, targetModel.Tools);
					summaryInput.Append(msg.Role).Append(": ").AppendLine(serialized).AppendLine();
				}

				var summaryMessage = new UserMessage(summaryInput.ToString());
				var summary = await Summarizer.Execute(summaryMessage, cancellationToken);
				LatestSummary = summary.Content;
				Messages.RemoveRange(0, messagesToSummarize.Count);

				totalTokens = 0;
				systemMessage = BuildSystemMessage();
				foreach (var msg in new IMessage[] { systemMessage }.Concat(Messages).Concat(messagesToAddList))
					totalTokens += await _tokenCountCache.GetAsync((msg, targetModel.Tools), cancellationToken);
			}

			Messages.AddRange(messagesToAddList);
			result.Add(systemMessage);
			result.AddRange(Messages);
			return result;
		}
	}
}