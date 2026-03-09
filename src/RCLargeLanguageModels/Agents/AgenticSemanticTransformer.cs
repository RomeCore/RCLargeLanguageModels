using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Exceptions;
using RCLargeLanguageModels.Messages;

namespace RCLargeLanguageModels.Agents
{
	/// <summary>
	/// Represents a semantic transformer that uses a LLM agent for transformation.
	/// </summary>
	public class AgenticSemanticTransformer : SemanticTransformer
	{
		/// <summary>
		/// The agent used for transformation. Can be <see langword="null"/> if no agent is available or if the transformer should operate in a pass-through mode.
		/// </summary>
		public LLMAgent? Agent { get; set; }

		public override async Task<string> TransformAsync(string input, CancellationToken cancellationToken = default)
		{
			var agent = Agent;
			if (agent != null)
			{
				var result = await agent.Execute(new UserMessage(input), cancellationToken);
				return result.Content ?? throw new LLMException("No content returned from agent.");
			}

			return input;
		}
	}
}