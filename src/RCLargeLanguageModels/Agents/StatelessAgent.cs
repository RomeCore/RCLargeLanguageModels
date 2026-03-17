using System;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Messages;

namespace RCLargeLanguageModels.Agents
{
	/// <summary>
	/// The simplest, stateless agent, with just 
	/// </summary>
	public class StatelessAgent : LLMAgent
	{
		/// <summary>
		/// Gets or sets the system instructions for the LLM.
		/// </summary>
		public string SystemInstructions { get; set; } = "You are a helpful assistant";

		/// <summary>
		/// Gets or sets the LLM provider that will be used for generation.
		/// </summary>
		public ILLMProvider? LLMProvider { get; set; }

		public override async Task<IAssistantMessage> Execute(IUserMessage userMessage, CancellationToken cancellationToken = default)
		{
			if (LLMProvider == null)
				throw new InvalidOperationException("LLM provider is not set.");

			var llm = LLMProvider.GetLLM();
			var messages = new IMessage[]
			{
				new SystemMessage(SystemInstructions),
				userMessage
			};
			return (await llm.ChatAsync(messages, cancellationToken: cancellationToken)).Message;
		}
	}
}