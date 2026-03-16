using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Messages;

namespace RCLargeLanguageModels.Agents
{
	/// <summary>
	/// Represents a stateless chat memory that just returns a pair of system + user message, without storing any information inside.
	/// </summary>
	public class StatelessChatMemory : LLMChatMemory
	{
		/// <summary>
		/// Gets or sets the system instructions for the LLM.
		/// </summary>
		public string SystemInstructions { get; set; } = "You are a helpful assistant.";

		public override Task<IEnumerable<IMessage>> AppendAsync(IUserMessage userMessage,
			LLModel targetModel, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(SystemInstructions))
				SystemInstructions = "You are a helpful assistant";
			return Task.FromResult<IEnumerable<IMessage>>(new IMessage[]
			{
				new SystemMessage(SystemInstructions),
				userMessage
			});
		}

		public override Task<IEnumerable<IMessage>> AppendAsync(IEnumerable<IMessage> previousMessages,
			IAssistantMessage assistantMessage, IEnumerable<IToolMessage> toolMessages,
			LLModel targetModel, CancellationToken cancellationToken = default)
		{
			var result = previousMessages.Append(assistantMessage)
				.Concat(toolMessages);
			return Task.FromResult<IEnumerable<IMessage>>(result);
		}
	}
}