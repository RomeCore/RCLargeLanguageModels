using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Messages.Attachments;

namespace RCLargeLanguageModels.Agents
{
	public abstract class LLMAgent
	{
		public LLMAgent()
		{

		}

		/// <summary>
		/// Gets the planner agent that will be used to plan the task execution.
		/// </summary>
		/// <returns>The planner agent or null if planning not needed.</returns>
		protected virtual LLMToolExecutionAgentBase? GetPlanner()
		{
			return null;
		}

		/// <summary>
		/// Executes the provided user message using the agentic system.
		/// </summary>
		/// <param name="userMessage">The current user's message.</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>The generated assistant message as an asynchronous operation.</returns>
		public async Task<IAssistantMessage> Execute(IUserMessage userMessage, CancellationToken cancellationToken = default)
		{
			LLMToolExecutionAgentBase planner = null!;

			return new AssistantMessage("");
		}
	}
}