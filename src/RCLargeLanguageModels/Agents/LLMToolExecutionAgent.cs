using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Messages;

namespace RCLargeLanguageModels.Agents
{
	/// <summary>
	/// Represents a large language model agent that executes tools.
	/// </summary>
	public class LLMToolExecutionAgent : LLMAgent
	{
		/// <summary>
		/// Gets or sets the tool executor that will be used to execute tools.
		/// </summary>
		public LLMToolExecutorBase? Executor { get; set; }

		public override async Task<IAssistantMessage> Execute(IUserMessage userMessage, CancellationToken cancellationToken = default)
		{
			if (Executor == null)
				throw new InvalidOperationException("Tool executor is not set.");

			var messages = await Executor.GenerateResponseAsync(userMessage, cancellationToken);
			var lastMessage = messages.LastOrDefault();
			return lastMessage as IAssistantMessage ?? throw new InvalidCastException($"Invalid message type: {lastMessage.GetType()}.");
		}
	}
}