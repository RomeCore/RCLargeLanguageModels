using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Messages;

namespace RCLargeLanguageModels.Agents
{
	/// <summary>
	/// Represents the RAG (Retrieval-Augmented Generation) provider that transforms a user message via injecting content (e.g. via a semantic search, or web search).
	/// </summary>
	public interface IRAGProvider
	{
		/// <summary>
		/// Transforms an input user message to inject content for RAG.
		/// </summary>
		/// <param name="userMessage">The input user message to inject content to.</param>
		/// <param name="cancellationToken">The cancellation token that can be used to cancel the operation.</param>
		/// <returns>The transformed input user message with content for RAG.</returns>
		Task<IUserMessage> TransformAsync(IUserMessage userMessage, CancellationToken cancellationToken = default);
	}
}