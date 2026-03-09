using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Messages.Attachments;

namespace RCLargeLanguageModels.Tools
{
	/// <summary>
	/// Represents the status of a tool execution.
	/// </summary>
	public enum ToolResultStatus
	{
		/// <summary>
		/// The tool execution was cancelled.
		/// </summary>
		Cancelled,

		/// <summary>
		/// The tool execution resulted in an error.
		/// </summary>
		Error,

		/// <summary>
		/// The tool execution was successful.
		/// </summary>
		Success,

		/// <summary>
		/// The tool did not produce any result.
		/// </summary>
		NoResult
	}

	/// <summary>
	/// Represents the result of a tool execution, containing the main content and optional attachments.
	/// </summary>
	public class ToolResult
	{
		/// <summary>
		/// Gets the status of the tool execution.
		/// </summary>
		public ToolResultStatus Status { get; }

		/// <summary>
		/// Gets or sets the main content of the tool result.
		/// </summary>
		public string Content { get; }

		/// <summary>
		/// Gets the collection of attachments associated with this tool result.
		/// </summary>
		public ImmutableList<IAttachment> Attachments { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ToolResult"/> class.
		/// </summary>
		public ToolResult()
		{
			Status = ToolResultStatus.Success;
			Content = string.Empty;
			Attachments = ImmutableList<IAttachment>.Empty;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ToolResult"/> class with a specified status.
		/// </summary>
		/// <param name="status">The status of the tool execution.</param>
		public ToolResult(ToolResultStatus status)
		{
			Status = status;
			Content = string.Empty;
			Attachments = ImmutableList<IAttachment>.Empty;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ToolResult"/> class with the specified content.
		/// </summary>
		/// <param name="content">The main content of the tool result.</param>
		public ToolResult(string content)
		{
			Status = ToolResultStatus.Success;
			Content = content ?? string.Empty;
			Attachments = ImmutableList<IAttachment>.Empty;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ToolResult"/> class with the specified attachments.
		/// </summary>
		/// <param name="attachments">The attachments to include in the tool result.</param>
		public ToolResult(IEnumerable<IAttachment> attachments)
		{
			Status = ToolResultStatus.Success;
			Content = string.Empty;
			Attachments = attachments.ToImmutableList();
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="ToolResult"/> class with the specified content and attachments.
		/// </summary>
		/// <param name="content">The main content of the tool result.</param>
		/// <param name="attachments">The attachments to include in the tool result.</param>
		public ToolResult(string content, IEnumerable<IAttachment> attachments)
		{
			Status = ToolResultStatus.Success;
			Content = content ?? string.Empty;
			Attachments = attachments.ToImmutableList();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ToolResult"/> class with the specified content.
		/// </summary>
		/// <param name="status">The status of the tool execution.</param>
		/// <param name="content">The main content of the tool result.</param>
		public ToolResult(ToolResultStatus status, string content)
		{
			Status = status;
			Content = content ?? string.Empty;
			Attachments = ImmutableList<IAttachment>.Empty;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ToolResult"/> class with the specified attachments.
		/// </summary>
		/// <param name="status">The status of the tool execution.</param>
		/// <param name="attachments">The attachments to include in the tool result.</param>
		public ToolResult(ToolResultStatus status, IEnumerable<IAttachment> attachments)
		{
			Status = status;
			Content = string.Empty;
			Attachments = attachments.ToImmutableList();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ToolResult"/> class with the specified content and attachments.
		/// </summary>
		/// <param name="status">The status of the tool execution.</param>
		/// <param name="content">The main content of the tool result.</param>
		/// <param name="attachments">The attachments to include in the tool result.</param>
		public ToolResult(ToolResultStatus status, string content, IEnumerable<IAttachment> attachments)
		{
			Status = status;
			Content = content ?? string.Empty;
			Attachments = attachments.ToImmutableList();
		}

		/// <summary>
		/// Implicitly converts a string to a ToolResult.
		/// </summary>
		/// <param name="content">The string content to convert.</param>
		public static implicit operator ToolResult(string content)
		{
			return new ToolResult(content);
		}
	}
}