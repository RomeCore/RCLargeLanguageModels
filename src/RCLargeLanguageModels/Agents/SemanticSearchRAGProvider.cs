using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Embeddings.Database;
using RCLargeLanguageModels.Messages;

namespace RCLargeLanguageModels.Agents
{
	/// <summary>
	/// Represents the RAG provider that adds content to user message by searching in <see cref="SemanticSector{T}"/>.
	/// </summary>
	public class SemanticSearchRAGProvider : RAGProvider
	{
		private readonly SemanticSector<string> _semSector;

		/// <summary>
		/// Gets or sets the maximum semantic search entries.
		/// </summary>
		public int MaxEntries { get; set; } = 10;

		/// <summary>
		/// Initializes a new instance of <see cref="SemanticSearchRAGProvider"/> class.
		/// </summary>
		/// <param name="semSector"></param>
		public SemanticSearchRAGProvider(SemanticSector<string> semSector)
		{
			_semSector = semSector;
		}

		public override async Task<IUserMessage> TransformAsync(IUserMessage userMessage, CancellationToken cancellationToken = default)
		{
			var entries = await _semSector.QueryAsync(userMessage.Content, maxCount: MaxEntries,
				cancellationToken: cancellationToken);

			if (entries.Length == 0)
				return userMessage;

			StringBuilder sb = new();

			sb.AppendLine("RAG context:");
			foreach (var entry in entries)
				sb.AppendLine(entry.Item);
			sb.AppendLine().AppendLine("User request:").Append(userMessage.Content);

			return new UserMessage(userMessage.Sender, sb.ToString(), userMessage.Attachments);
		}
	}
}