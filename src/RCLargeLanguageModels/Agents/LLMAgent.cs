using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Messages.Attachments;

namespace RCLargeLanguageModels.Agents
{
	/// <summary>
	/// Represents a large language model agent.
	/// </summary>
	public abstract class LLMAgent
	{
		/// <summary>
		/// Executes the provided user message using the agentic system.
		/// </summary>
		/// <param name="userMessage">The current user's message.</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>The generated assistant message as an asynchronous operation.</returns>
		public abstract Task<IAssistantMessage> Execute(IUserMessage userMessage, CancellationToken cancellationToken = default);
	}
}