using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

		/// <summary>
		/// The default summarization prompt used by the <see cref="CreateSummarizer(ILLMProvider)"/>.
		/// </summary>
		public const string DefaultSummarizationPrompt = @"You are a conversation memory summarizer used inside an AI system.

Your task is to compress a conversation into a concise but information-dense summary that will be used as long-term memory for future interactions.

CRITICAL REQUIREMENTS:

1. Preserve all important information:
	- User goals, intents, and requests
	- Key facts and constraints
	- Decisions that were made
	- Unresolved questions or tasks
	- Important context needed for future responses

2. Preserve technical details when present:
	- Code logic, architecture decisions, APIs
	- Error messages and their causes
	- Tool usage results and outcomes

3. Maintain continuity:
	- If a previous summary is provided, integrate it with new information
	- Do NOT repeat information unnecessarily
	- Do NOT contradict earlier context

4. Remove unimportant content:
	- Small talk
	- Repetitions
	- Irrelevant details

5. Be structured and compact:
	- Use clear sections if helpful
	- Prefer bullet points for dense information
	- Avoid long prose

6. Be precise:
	- Do not generalize important details
	- Do not invent information
	- Do not omit critical steps in reasoning or workflow

7. Tool usage handling:
	- If tools were used, include:
		- What tool was used
		- Why it was used
		- The result of the tool

OUTPUT FORMAT:

Return only the summary text.

The summary should be compact but complete enough so that another AI can continue the conversation without needing the original messages.";

		/// <summary>
		/// Creates the summarizer agent from the given LLM provider.
		/// </summary>
		/// <param name="llm">The LLM provider.</param>
		/// <returns>The summarizer agent.</returns>
		public static LLMAgent CreateSummarizer(ILLMProvider llm)
		{
			return new StatelessAgent
			{
				SystemInstructions = DefaultSummarizationPrompt,
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
		/// Gets or sets the list of latest summarized messages.
		/// This list contains the messages that was removed from <see cref="Messages"/> upon last summarization.
		/// </summary>
		public ImmutableList<IMessage> LatestSummarizedMessages { get; set; } = ImmutableList<IMessage>.Empty;

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
		/// Default is <see cref="Statistics.TokenCounter.Naive"/>, which counts tokens by dividing count of characters by 2.5.
		/// </summary>
		public ITokenCounter TokenCounter { get; set; } = Statistics.TokenCounter.Naive;

		/// <summary>
		/// Gets or sets the serialization schema used for serializing messages for token counting and summary generation.
		/// </summary>
		public IMessageTokenSerializationSchema MessageSerializationSchema { get; set; } = Statistics.MessageTokenSerializationSchema.Default;

		/// <summary>
		/// Gets or sets the maximum context window in tokens that target LLM model should support (default is 128000).
		/// This value will be multiplied by <see cref="Tolerance"/> and used for summarization trigger.
		/// </summary>
		public int TargetTokens { get; set; } = 128000;

		/// <summary>
		/// Gets or sets the maximum number of tokens that summarizer agent can process (default is 128000).
		/// This value will be multiplied by <see cref="Tolerance"/> and used for summarization message selection.
		/// </summary>
		public int MaxSummarizerTokens { get; set; } = 128000;

		/// <summary>
		/// Gets or sets the summarization trigger tokens tolerance multiplier, default is 0.7 or 70%.
		/// </summary>
		public float Tolerance { get; set; } = 0.7f;

		/// <summary>
		/// Gets or sets the count of last messages that will not be included in summarization sequence.
		/// </summary>
		public int KeepLastNMessages { get; set; } = 0;

		/// <summary>
		/// Initializes a new instance of <see cref="SummarizingChatMemory"/> class.
		/// </summary>
		public SummarizingChatMemory()
		{
			_tokenCountCache = new((t, ct) =>
			{
				return TokenCounter.CountAsync(t.Item1, t.Item2, MessageSerializationSchema, ct);
			}, slidingExpirationTime: TimeSpan.FromHours(1));
		}

		/// <summary>
		/// Initializes a new instance of <see cref="SummarizingChatMemory"/> class.
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

		protected virtual ISystemMessage BuildSystemMessage()
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
			int maxSummarizerTokens = (int)(MaxSummarizerTokens * Tolerance);

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

				int summaryInputTokens = 0;
				if (!string.IsNullOrWhiteSpace(LatestSummary))
				{
					summaryInput.Append("Previous messages summary: ").AppendLine(LatestSummary).AppendLine();
					summaryInputTokens = await TokenCounter.CountAsync(LatestSummary, cancellationToken);
				}

				for (int i = 0; i < Messages.Count - KeepLastNMessages; i++)
				{
					var message = Messages[i];
					int currentTokens = await _tokenCountCache.GetAsync((message, targetModel.Tools), cancellationToken);
					if (summaryInputTokens + currentTokens > maxSummarizerTokens)
					{
						break;
					}
					messagesToSummarize.Add(message);
					summaryInputTokens += currentTokens;
				}

				// Remove the assistant-tool messages if the number of tool messages is not equal to the number of assistant tool calls
				if (messagesToSummarize.Count > 0 &&
					messagesToSummarize[messagesToSummarize.Count - 1] is IToolMessage)
				{
					int toolMsgCount = 1;
					while (true)
					{
						if (messagesToSummarize.Count > toolMsgCount + 1)
						{
							var lastMsg = messagesToSummarize[messagesToSummarize.Count - toolMsgCount - 1];
							if (lastMsg is IToolMessage)
							{
								toolMsgCount++;
							}
							else if (lastMsg is IAssistantMessage assistMsg1)
							{
								if (assistMsg1.ToolCalls.Count != toolMsgCount)
								{
									messagesToSummarize.RemoveRange(messagesToSummarize.Count - 2 - toolMsgCount,
										toolMsgCount + 1);
								}

								break;
							}
						}
						else
						{
							// Messages to summarize contains ENTIRELY tool messages, throw exception later
							messagesToSummarize.Clear();
							break;
						}
					}
				}

				// Remove last assistant message if it contains some tool calls
				if (messagesToSummarize.Count > 0 &&
					messagesToSummarize[messagesToSummarize.Count - 1] is IAssistantMessage assistMsg2)
				{
					if (assistMsg2.ToolCalls.Count > 0)
					{
						messagesToSummarize.RemoveAt(messagesToSummarize.Count - 1);
					}
				}

				// Remove last user message
				if (messagesToSummarize.Count > 0 &&
					messagesToSummarize[messagesToSummarize.Count - 1] is IUserMessage)
				{
					messagesToSummarize.RemoveAt(messagesToSummarize.Count - 1);
				}

				// Append last summarizing user message to the end if there will not be any user messages in result conversation
				if (messagesToSummarize.OfType<IUserMessage>().LastOrDefault() is IUserMessage userMsg &&
					!Messages.Concat(messagesToAddList).Any(m => m is IUserMessage))
				{
					messagesToAddList.Insert(0, userMsg);
				}

				if (messagesToSummarize.Count == 0)
					throw new Exception($"No messages to summarize. This may be caused by too low limit for summarizer context window or too long messages " +
						$"(see {nameof(MaxSummarizerTokens)} and {nameof(TargetTokens)} properties). " +
						$"Please increase these limits or adjust your model's context window settings.");

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
				LatestSummarizedMessages = messagesToSummarize.ToImmutableList();
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