using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Messages;

namespace RCLargeLanguageModels.Agents
{
	/// <summary>
	/// Represents the RAG (Retrieval-Augmented Generation) provider that transforms a user message via injecting content (e.g. via a semantic search, or web search).
	/// </summary>
	public abstract class RAGProvider : IRAGProvider
	{
		public abstract Task<IUserMessage> TransformAsync(IUserMessage userMessage, CancellationToken cancellationToken = default);
	
		private class PassThroughRAGProvider : RAGProvider
		{
			public override Task<IUserMessage> TransformAsync(IUserMessage userMessage, CancellationToken cancellationToken = default)
			{
				return Task.FromResult(userMessage);
			}
		}

		/// <summary>
		/// Gets the simple <see cref="RAGProvider"/> instance that just returns input user message without any transformations.
		/// </summary>
		public static RAGProvider PassThrough { get; } = new PassThroughRAGProvider();
	}
}