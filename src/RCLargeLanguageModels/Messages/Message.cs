using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Messages.Attachments;
using RCLargeLanguageModels.Metadata;
using RCLargeLanguageModels.Tools;

namespace RCLargeLanguageModels.Messages
{
	/// <summary>
	/// Represents a message in the chat with LLM. This can be a system message, user message, assistant message, or tool message.
	/// </summary>
	public class Message : ISystemMessage, IUserMessage, IAssistantMessage, IToolMessage
	{
		public Role Role { get; set; }

		public string? Sender { get; set; }

		public string? ReasoningContent { get; set; }

		public string? Content { get; set; }

		public IReadOnlyList<IToolCall> ToolCalls { get; set; } = Array.Empty<IToolCall>();

		public IReadOnlyList<IAttachment> Attachments { get; set; } = Array.Empty<IAttachment>();

		public IMetadataCollection Metadata { get; set; } = MetadataCollection.Empty;

		public IReadOnlyList<IMetadata> PartialMetadata { get; set; } = Array.Empty<IMetadata>();

		public IEnumerable<ITokenProbabilitiesMetadata> TokenProbabilities
			=> PartialMetadata?.OfType<TokenProbabilitiesMetadata>() ?? Enumerable.Empty<ITokenProbabilitiesMetadata>();

		public IFinishReasonMetadata? FinishReason => Metadata?.TryGet<FinishReasonMetadata>();

		public ToolResultStatus ToolStatus { get; set; } = ToolResultStatus.Success;
		ToolResultStatus IToolMessage.Status => ToolStatus;

		public ToolResult ToolResult => new ToolResult(ToolStatus, Content ?? string.Empty, Attachments ?? Enumerable.Empty<IAttachment>());
		ToolResult IToolMessage.Result => ToolResult;

		public string ToolName { get; set; } = string.Empty;

		public string ToolCallId { get; set; } = string.Empty;

		/// <summary>
		/// Converts the current message to a roled message. This is useful when you want to ensure that the message has a specific role.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public IMessage ToRoledMessage()
		{
			return Role switch
			{
				Role.System => new SystemMessage(Content ?? string.Empty),
				Role.User => new UserMessage(Sender ?? string.Empty, Content ?? string.Empty, Attachments),
				Role.Assistant => new AssistantMessage(Content, ReasoningContent, ToolCalls, Attachments, PartialMetadata, Metadata),
				Role.Tool => new ToolMessage(ToolResult, ToolName, ToolCallId),
				_ => throw new InvalidOperationException("Message must have a valid role."),
			};
		}
	}

	/// <summary>
	/// Extension methods for the Message class.
	/// </summary>
	public static class MessageExtension
	{
		/// <summary>
		/// Converts the current message to a general message. This is useful when you want to ensure that the message has a specific role.
		/// </summary>
		/// <param name="message">The current message.</param>
		/// <returns>A general message with the specified role.</returns>
		public static Message ToGeneralMessage(this IMessage message)
		{
			var result = new Message();

			if (message is null)
				return result;

			result.Role = message.Role;
			result.Content = message.Content;

			if (message is IAttachmentsMessage attachmentsMessage)
			{
				result.Attachments = attachmentsMessage.Attachments;
			}

			if (message is IAssistantMessage assistantMessage)
			{
				result.ReasoningContent = assistantMessage.ReasoningContent;
				result.ToolCalls = assistantMessage.ToolCalls;
			}

			if (message is IToolMessage toolMessage)
			{
				result.ToolName = toolMessage.ToolName;
				result.ToolCallId = toolMessage.ToolCallId;
			}

			if (message is ICompletion completion)
			{
				result.Metadata = completion.Metadata;
				result.PartialMetadata = completion.PartialMetadata;
			}

			return result;
		}
	}
}