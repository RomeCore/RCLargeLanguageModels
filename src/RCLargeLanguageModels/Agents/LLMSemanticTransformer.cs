using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Exceptions;
using RCLargeLanguageModels.Messages;

namespace RCLargeLanguageModels.Agents
{
	/// <summary>
	/// Represents a semantic transformer that uses an LLM (Large Language Model) for <see cref="string"/> transformation.
	/// </summary>
	public class LLMSemanticTransformer : SemanticTransformer<string, string>
	{
		/// <summary>
		/// The messages to put into the model for transformation.
		/// </summary>
		public IEnumerable<IMessage> Messages { get; set; } = Enumerable.Empty<IMessage>();

		/// <summary>
		/// The model used for transformation. Can be <see langword="null"/> if no model is available or if the transformer should operate in a pass-through mode.
		/// </summary>
		public ILLMProvider? Model { get; set; }

		public override async Task<string> TransformAsync(string input, CancellationToken cancellationToken = default)
		{
			var model = Model?.GetLLM();
			if (model != null)
			{
				var messages = new List<IMessage>(Messages)
				{
					new UserMessage(input)
				};
				var result = await model.ChatAsync(messages, cancellationToken: cancellationToken);
				return result.Content ?? throw new LLMException("No content returned from agent.");
			}

			return input;
		}
	}
}