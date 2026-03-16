using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Statistics;

namespace RCLargeLanguageModels.Agents
{
	/// <summary>
	/// The LLM chat memory that compresses context by summarizing old messages.
	/// </summary>
	public class SummarizingChatMemory : LLMChatMemory
	{
		/// <summary>
		/// Gets or sets the system instructions.
		/// </summary>
		public string SystemInstructions { get; set; } = "You are a helpful assistant.";

		/// <summary>
		/// Gets or sets the list of messages. All added system messages will be removed and appended into <see cref="SystemInstructions"/>.
		/// </summary>
		public List<IMessage> Messages { get; set; } = new();

		/// <summary>
		/// Gets or sets the summary map, where keys are last messages that been included in summary seqeunce, and values are summaries.
		/// </summary>
		public Dictionary<IMessage, string> SummaryMap { get; set; } = new();

		/// <summary>
		/// Gets or sets the summarizer agent that will be used for summarization.
		/// </summary>
		public LLMAgent? Summarizer { get; set; }

		/// <summary>
		/// Gets or sets the token counter used for summarization triggering.
		/// </summary>
		public ITokenCounter TokenCounter { get; set; } = Statistics.TokenCounter.Naive;

		/// <summary>
		/// Gets or sets the serialization schema used for serializing messages for token counting.
		/// </summary>
		public IMessageTokenSerializationSchema MessageTokenSerializationSchema { get; set; } = Statistics.MessageTokenSerializationSchema.Default;

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

		/*
		 * АЛГОРИТМ:
		 * 
		 * При формировании результата мы проверяем сообщения на количество токенов
		 * Если токены больше заданного предела -> суммаризируем
		 * При суммаризации не учитывается системное сообщение + последние N сообщений
		 * Суммаризация может проводиться несколько раз подряд, если сообщений слишком много
		 */

		public override Task<IEnumerable<IMessage>> AppendAsync(IUserMessage userMessage,
			LLModel targetModel, CancellationToken cancellationToken = default)
		{
			throw new System.NotImplementedException();
		}

		public override Task<IEnumerable<IMessage>> AppendAsync(IEnumerable<IMessage> previousMessages, IAssistantMessage assistantMessage,
			IEnumerable<IToolMessage> toolMessages, LLModel targetModel, CancellationToken cancellationToken = default)
		{
			throw new System.NotImplementedException();
		}
	}
}