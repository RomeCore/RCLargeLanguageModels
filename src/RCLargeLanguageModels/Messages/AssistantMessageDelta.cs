using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using RCLargeLanguageModels.Messages.Attachments;
using RCLargeLanguageModels.Metadata;
using RCLargeLanguageModels.Tools;

namespace RCLargeLanguageModels.Messages
{
	/// <summary>
	/// Represents a message delta for a partial assistant message.
	/// </summary>
	public class AssistantMessageDelta
	{
		/// <summary>
		/// The delta content of the assistant message. Can be <see langword="null"/>.
		/// </summary>
		public string? DeltaContent { get; }

		/// <summary>
		/// The delta reasoning content of the assistant message. Can be <see langword="null"/>.
		/// </summary>
		public string? DeltaReasoningContent { get; }

		/// <summary>
		/// The new tool calls for the assistant message. Can be <see langword="null"/>.
		/// </summary>
		public ImmutableList<IToolCall>? NewToolCalls { get; }

		/// <summary>
		/// The new attachments for the assistant message. Can be <see langword="null"/>.
		/// </summary>
		public ImmutableList<IAttachment>? NewAttachments { get; }

		/// <summary>
		/// The new partial metadata for the assistant message. Can be <see langword="null"/>.
		/// </summary>
		public ImmutableList<IMetadata>? NewPartialMetadata { get; }

		/// <summary>
		/// Gets the value indicating whether this delta can be condered empty.
		/// </summary>
		public bool IsEmpty =>
			string.IsNullOrEmpty(DeltaContent) &&
			string.IsNullOrEmpty(DeltaReasoningContent) &&
			NewToolCalls.IsNullOrEmpty() &&
			NewAttachments.IsNullOrEmpty() &&
			NewPartialMetadata.IsNullOrEmpty();

		/// <summary>
		/// Initializes a new instance of <see cref="AssistantMessageDelta"/> class.
		/// </summary>
		/// <param name="deltaContent"></param>
		public AssistantMessageDelta(string deltaContent)
		{
			DeltaContent = deltaContent;
			DeltaReasoningContent = null;
			NewToolCalls = null;
			NewAttachments = null;
			NewPartialMetadata = null;
		}
		
		/// <summary>
		/// Initializes a new instance of <see cref="AssistantMessageDelta"/> class.
		/// </summary>
		/// <param name="deltaContent"></param>
		/// <param name="deltaReasoningContent"></param>
		public AssistantMessageDelta(string deltaContent, string deltaReasoningContent)
		{
			DeltaContent = deltaContent;
			DeltaReasoningContent = deltaReasoningContent;
			NewToolCalls = null;
			NewAttachments = null;
			NewPartialMetadata = null;
		}

		/// <summary>
		/// Initializes a new instance of <see cref="AssistantMessageDelta"/> class.
		/// </summary>
		/// <param name="deltaContent"></param>
		/// <param name="deltaReasoningContent"></param>
		/// <param name="newToolCalls"></param>
		/// <param name="newAttachments"></param>
		/// <param name="newPartialMetadata"></param>
		public AssistantMessageDelta(
			string? deltaContent = null,
			string? deltaReasoningContent = null,
			IEnumerable<IToolCall>? newToolCalls = null,
			IEnumerable<IAttachment>? newAttachments = null,
			IEnumerable<IMetadata>? newPartialMetadata = null)
		{
			DeltaContent = deltaContent;
			DeltaReasoningContent = deltaReasoningContent;
			NewToolCalls = newToolCalls?.ToImmutableList();
			NewAttachments = newAttachments?.ToImmutableList();
			NewPartialMetadata = newPartialMetadata?.ToImmutableList();
		}
		
		public static AssistantMessageDelta Content(string deltaContent)
		{
			if (deltaContent == null)
				throw new ArgumentNullException(nameof(deltaContent));
			return new AssistantMessageDelta(deltaContent: deltaContent);
		}

		public static AssistantMessageDelta ReasoningContent(string deltaReasoningContent)
		{
			if (deltaReasoningContent == null)
				throw new ArgumentNullException(nameof(deltaReasoningContent));
			return new AssistantMessageDelta(deltaReasoningContent: deltaReasoningContent);
		}

		public static AssistantMessageDelta ToolCall(IToolCall toolCall)
		{
			if (toolCall == null)
				throw new ArgumentNullException(nameof(toolCall));
			return new AssistantMessageDelta(newToolCalls: new IToolCall[] { toolCall });
		}

		public static AssistantMessageDelta ToolCalls(IEnumerable<IToolCall> toolCalls)
		{
			if (toolCalls == null)
				throw new ArgumentNullException(nameof(toolCalls));
			return new AssistantMessageDelta(newToolCalls: toolCalls.Where(tc => tc != null));
		}

		public static AssistantMessageDelta Attachment(IAttachment attachment)
		{
			if (attachment == null)
				throw new ArgumentNullException(nameof(attachment));
			return new AssistantMessageDelta(newAttachments: new IAttachment[] { attachment });
		}

		public static AssistantMessageDelta Attachments(IEnumerable<IAttachment> attachments)
		{
			if (attachments == null)
				throw new ArgumentNullException(nameof(attachments));
			return new AssistantMessageDelta(newAttachments: attachments.Where(a => a != null));
		}

		public static AssistantMessageDelta Metadata(IMetadata metadata)
		{
			if (metadata == null)
				throw new ArgumentNullException(nameof(metadata));
			return new AssistantMessageDelta(newPartialMetadata: new IMetadata[] { metadata });
		}

		public static AssistantMessageDelta Metadata(IEnumerable<IMetadata> metadatas)
		{
			if (metadatas == null)
				throw new ArgumentNullException(nameof(metadatas));
			return new AssistantMessageDelta(newPartialMetadata: metadatas.Where(m => m != null));
		}

		public static implicit operator string(AssistantMessageDelta delta) => delta.DeltaContent ?? string.Empty;

		public override string ToString()
		{
			return DeltaContent ?? string.Empty;
		}
	}
}