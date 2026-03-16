using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Messages;

namespace RCLargeLanguageModels.Agents
{
	/// <summary>
	/// Represents the LLM chat memory that simply appends messages and returns last N messages.
	/// </summary>
	public class SlidingChatMemory : LLMChatMemory
	{
		/// <summary>
		/// Gets or sets the system instructions for LLM.
		/// </summary>
		public string SystemInstructions { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the list of messages. All added system messages will be removed and appended into <see cref="SystemInstructions"/>.
		/// </summary>
		public List<IMessage> Messages { get; set; } = new();

		/// <summary>
		/// Gets or sets the maximum count of messages that will be returned. Use negative values to disable this limit.
		/// </summary>
		public int ReturnLastNMessages { get; set; } = -1;

		private Task<IEnumerable<IMessage>> Append(IEnumerable<IMessage> messagesToAdd)
		{
			if (string.IsNullOrWhiteSpace(SystemInstructions))
				SystemInstructions = "You are a helpful assistant";
			Messages ??= new();
			var maxMessages = ReturnLastNMessages;

			// Move contents of system messages to system instructions
			for (int i = 0; i < Messages.Count; i++)
			{
				var message = Messages[i];
				if (message is ISystemMessage && !string.IsNullOrWhiteSpace(message.Content))
				{
					if (string.IsNullOrWhiteSpace(SystemInstructions))
						SystemInstructions = message.Content;
					else
						SystemInstructions += "\n\n" + message.Content;
					Messages.RemoveAt(i);
					i--;
				}
			}

			var systemMessage = new SystemMessage(SystemInstructions);
			List<IMessage> result = new()
			{
				systemMessage
			};
			Messages.AddRange(messagesToAdd);
			result.AddRange(Messages);
			if (maxMessages >= 0 && result.Count - 1 > maxMessages)
				result.RemoveRange(1, result.Count - 1 - maxMessages);

			// Remove headless tool messages (the tool messages without tool call before them)
			// The tool calls contined in previous assistant message
			while (result.Count > 1 && result[1] is IToolMessage)
				result.RemoveAt(1);

			return Task.FromResult<IEnumerable<IMessage>>(result);
		}

		public override Task<IEnumerable<IMessage>> AppendAsync(IUserMessage userMessage,
			LLModel targetModel, CancellationToken cancellationToken = default)
		{
			return Append(new IMessage[] { userMessage });
		}

		public override Task<IEnumerable<IMessage>> AppendAsync(IEnumerable<IMessage> previousMessages, IAssistantMessage assistantMessage,
			IEnumerable<IToolMessage> toolMessages, LLModel targetModel, CancellationToken cancellationToken = default)
		{
			return Append(new IMessage[] { assistantMessage }.Concat(toolMessages));
		}
	}
}