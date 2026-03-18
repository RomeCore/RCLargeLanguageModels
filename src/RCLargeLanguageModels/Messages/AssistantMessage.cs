using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Messages.Attachments;
using RCLargeLanguageModels.Metadata;
using RCLargeLanguageModels.Tools;

namespace RCLargeLanguageModels.Messages
{
	/// <summary>
	/// Represents an immutable, completed AI assistant's message in a chat with LLM.
	/// </summary>
	public class AssistantMessage : IAssistantMessage
	{
		public Role Role => Role.Assistant;

		/// <inheritdoc/>
		public string? Content { get; }

		/// <inheritdoc/>
		public string? ReasoningContent { get; }

		/// <inheritdoc cref="IAssistantMessage.ToolCalls"/>
		public ImmutableArray<IToolCall> ToolCalls { get; }
		IReadOnlyList<IToolCall> IAssistantMessage.ToolCalls => ToolCalls;

		/// <inheritdoc cref="IAttachmentsMessage.Attachments"/>
		public ImmutableArray<IAttachment> Attachments { get; }
		IReadOnlyList<IAttachment> IAttachmentsMessage.Attachments => Attachments;

		/// <inheritdoc cref="ICompletion.PartialMetadata"/>
		public ImmutableArray<IMetadata> PartialMetadata { get; }
		IReadOnlyList<IMetadata> ICompletion.PartialMetadata => PartialMetadata;

		/// <inheritdoc cref="ICompletion.Metadata"/>
		public MetadataCollection Metadata { get; }
		IMetadataCollection IMetadataProvider.Metadata => Metadata;
		IMetadataCollection ICompletion.Metadata => Metadata;

		/// <inheritdoc/>
		public IEnumerable<ITokenProbabilitiesMetadata> TokenProbabilities
			=> PartialMetadata.OfType<ITokenProbabilitiesMetadata>();

		/// <inheritdoc/>
		public IFinishReasonMetadata? FinishReason => Metadata.TryGet<IFinishReasonMetadata>();

		/// <summary>
		/// Creates a new instance of <see cref="AssistantMessage"/> class.
		/// </summary>
		/// <param name="content">The assistant message content.</param>
		public AssistantMessage(string content)
			: this(content, null)
		{
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="AssistantMessage"/> class.
		/// </summary>
		/// <param name="content">The assistant message content.</param>
		/// <param name="reasoningContent">The assistant message reasoning content. Can be <see langword="null"/>.</param>
		public AssistantMessage(
			string? content,
			string? reasoningContent)
			: this(content, reasoningContent, null, null, null, null)
		{
		}

		/// <summary>
		/// Creates a new instance of <see cref="AssistantMessage"/> class.
		/// </summary>
		/// <param name="content">The assistant message content.</param>
		/// <param name="reasoningContent">The assistant message reasoning content. Can be <see langword="null"/>.</param>
		/// <param name="toolCalls">The tool calls for this message.</param>
		/// <param name="attachments">The attachments for this message.</param>
		/// <param name="partialMetadata">The partial metadata for this message.</param>
		/// <param name="completionMetadata">The completion metadata for this message.</param>
		public AssistantMessage(
			string? content = null,
			string? reasoningContent = null,
			IEnumerable<IToolCall>? toolCalls = null,
			IEnumerable<IAttachment>? attachments = null,
			IEnumerable<IMetadata>? partialMetadata = null,
			IEnumerable<IMetadata>? completionMetadata = null)
		{
			Content = content;
			ReasoningContent = reasoningContent;
			ToolCalls = toolCalls?.ToImmutableArray() ?? ImmutableArray<IToolCall>.Empty;
			Attachments = attachments?.ToImmutableArray() ?? ImmutableArray<IAttachment>.Empty;
			PartialMetadata = partialMetadata?.ToImmutableArray() ?? ImmutableArray<IMetadata>.Empty;
			Metadata = completionMetadata != null
				? new MetadataCollection(completionMetadata)
				: MetadataCollection.Empty;
		}

		/// <summary>
		/// Creates a new instance of <see cref="AssistantMessage"/> class.
		/// </summary>
		/// <param name="content">The assistant message content.</param>
		/// <param name="reasoningContent">The assistant message reasoning content. Can be <see langword="null"/>.</param>
		/// <param name="toolCalls">The tool calls for this message.</param>
		/// <param name="attachments">The attachments for this message.</param>
		/// <param name="partialMetadata">The partial metadata for this message.</param>
		/// <param name="completionMetadata">The completion metadata for this message.</param>
		public AssistantMessage(
			string? content = null,
			string? reasoningContent = null,
			ImmutableArray<IToolCall> toolCalls = default,
			ImmutableArray<IAttachment> attachments = default,
			ImmutableArray<IMetadata> partialMetadata = default,
			MetadataCollection? completionMetadata = null)
		{
			Content = content ?? string.Empty;
			ReasoningContent = reasoningContent ?? string.Empty;
			ToolCalls = toolCalls;
			Attachments = attachments;
			PartialMetadata = partialMetadata;
			Metadata = completionMetadata ?? MetadataCollection.Empty;
		}

		/// <summary>
		/// Converts the assistant message to a partial assistant message.
		/// </summary>
		/// <returns></returns>
		public PartialAssistantMessage AsPartialMessage()
		{
			return new PartialAssistantMessage(Content, ReasoningContent, ToolCalls, Attachments, PartialMetadata, Metadata);
		}
	}
}