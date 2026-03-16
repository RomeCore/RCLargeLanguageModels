using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Messages;

namespace RCLargeLanguageModels.Agents
{
	/// <summary>
	/// Represents a mutable chat memory that provides a collection of messages for LLM API.
	/// </summary>
	public abstract class LLMChatMemory
	{
		/// <summary>
		/// Appends the user message to the merory and returns the complete conversation for LLM API.
		/// Conversation turn (user request -&gt; LLM response) begins here.
		/// </summary>
		/// <remarks>
		/// Messages there can be transformed, summarized and trimmed, also RAG content can be provided.
		/// </remarks>
		/// <param name="userMessage">The user message to add.</param>
		/// <param name="targetModel">The LLM that currently used for generating response with result messages.</param>
		/// <param name="cancellationToken">The cancellation token that can be used to cancel the operation.</param>
		/// <returns></returns>
		public abstract Task<IEnumerable<IMessage>> AppendAsync(IUserMessage userMessage,
			LLModel targetModel, CancellationToken cancellationToken = default);

		/// <summary>
		/// Appends the assistant message with tool call answers and returns the complete conversation for LLM API.
		/// </summary>
		/// <remarks>
		/// Messages there can be transformed, summarized and trimmed.
		/// </remarks>
		/// <param name="previousMessages">The previous messages that returned this chat memory instance.</param>
		/// <param name="assistantMessage">The assistant message to add.</param>
		/// <param name="toolMessages">The tool messages that answers assistant's message tool calls. Should be added along with assistant message.</param>
		/// <param name="targetModel">The LLM that currently used for generating response with result messages.</param>
		/// <param name="cancellationToken">The cancellation token that can be used to cancel the operation.</param>
		/// <returns></returns>
		public abstract Task<IEnumerable<IMessage>> AppendAsync(IEnumerable<IMessage> previousMessages,
			IAssistantMessage assistantMessage, IEnumerable<IToolMessage> toolMessages,
			LLModel targetModel, CancellationToken cancellationToken = default);
	}
}