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

		public IReadOnlyList<IToolCall> ToolCalls { get; set; }

		public IReadOnlyList<IAttachment> Attachments { get; set; }

		public IMetadataCollection Metadata { get; set; }

		public IReadOnlyList<IMetadata> PartialMetadata { get; set; }

		public IEnumerable<ITokenProbabilitiesMetadata> TokenProbabilities
			=> PartialMetadata?.OfType<TokenProbabilitiesMetadata>() ?? Enumerable.Empty<ITokenProbabilitiesMetadata>();

		public IStopReasonMetadata? StopReason => Metadata?.TryGet<StopReasonMetadata>();

		public string ToolName { get; set; }

		public string ToolCallId { get; set; }

		/// <summary>
		/// Converts the current message to a roled message. This is useful when you want to ensure that the message has a specific role.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public IMessage ToRoledMessage()
		{
			switch (Role)
			{
				case Role.System:
					return new SystemMessage(Content);

				case Role.User:
					return new UserMessage(Sender, Content, Attachments);

				case Role.Assistant:
					return new AssistantMessage(Content, ReasoningContent, ToolCalls, Attachments, PartialMetadata, Metadata);

				case Role.Tool:
					return new ToolMessage(Content, ToolName, ToolCallId, Attachments);

				default:
					throw new InvalidOperationException("Message must have a valid role.");
			}
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