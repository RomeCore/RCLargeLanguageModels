using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Newtonsoft.Json;
using RCLargeLanguageModels.Messages.Attachments;
using RCLargeLanguageModels.Tools;

namespace RCLargeLanguageModels.Messages
{
	/// <summary>
	/// Represents a tool message that can be used to answer to the tool call.
	/// </summary>
	public class ToolMessage : IToolMessage
	{
		public Role Role => Role.Tool;

		/// <summary>
		/// The tool message content.
		/// </summary>
		public string Content { get; }

		/// <summary>
		/// The tool call identifier used to response to the right tool call.
		/// Used by most LLMs to bind this message to the respective tool call.
		/// </summary>
		public string ToolCallId { get; }

		/// <summary>
		/// The source tool name that is used to reponse to the right tool call.
		/// Used by some LLMs for understanding what the tool call is this message for.
		/// </summary>
		public string ToolName { get; }

		/// <summary>
		/// Gets the attachments list of the tool message.
		/// </summary>
		public IReadOnlyList<IAttachment> Attachments { get; }

		/// <summary>
		/// Creates a new instance of <see cref="ToolMessage"/> class.
		/// </summary>
		/// <param name="content">The content of the tool message.</param>
		/// <param name="toolCallId">The tool call identifier.</param>
		/// <param name="toolName">The source tool name.</param>
		public ToolMessage(string content, string toolCallId, string toolName)
		{
			Content = content ?? throw new ArgumentNullException(nameof(content));
			ToolCallId = toolCallId ?? throw new ArgumentNullException(nameof(toolCallId));
			ToolName = toolName ?? throw new ArgumentNullException(nameof(toolName));
			Attachments = Array.Empty<IAttachment>();
		}

		/// <summary>
		/// Creates a new instance of <see cref="ToolMessage"/> class.
		/// </summary>
		/// <param name="result">The full content of the tool message.</param>
		/// <param name="toolCallId">The tool call identifier.</param>
		/// <param name="toolName">The source tool name.</param>
		public ToolMessage(ToolResult result, string toolCallId, string toolName)
		{
			if (result == null) throw new ArgumentNullException(nameof(result));

			Content = result.Content;
			ToolCallId = toolCallId ?? throw new ArgumentNullException(nameof(toolCallId));
			ToolName = toolName ?? throw new ArgumentNullException(nameof(toolName));
			Attachments = result.Attachments.ToImmutableList();
		}

		/// <summary>
		/// Creates a new instance of <see cref="ToolMessage"/> class with the specified attachments.
		/// </summary>
		/// <param name="content">The content of the tool message.</param>
		/// <param name="toolCallId">The tool call identifier.</param>
		/// <param name="toolName">The source tool name.</param>
		/// <param name="attachments">The attachments of the message.</param>
		public ToolMessage(string content, string toolCallId, string toolName, IEnumerable<IAttachment>? attachments)
		{
			Content = content ?? throw new ArgumentNullException(nameof(content));
			ToolCallId = toolCallId ?? throw new ArgumentNullException(nameof(toolCallId));
			ToolName = toolName ?? throw new ArgumentNullException(nameof(toolName));
			Attachments = attachments?.ToImmutableList() ?? throw new ArgumentNullException(nameof(attachments));
		}
	}
}