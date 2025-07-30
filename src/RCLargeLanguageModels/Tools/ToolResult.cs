using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Messages.Attachments;

namespace RCLargeLanguageModels.Tools
{
	/// <summary>
	/// Represents the result of a tool execution, containing the main content and optional attachments.
	/// </summary>
	public class ToolResult
	{
		private readonly List<IAttachment> _attachments = new List<IAttachment>();

		/// <summary>
		/// Gets or sets the main content of the tool result.
		/// </summary>
		public string Content { get; }

		/// <summary>
		/// Gets the collection of attachments associated with this tool result.
		/// </summary>
		public IReadOnlyCollection<IAttachment> Attachments => _attachments.AsReadOnly();

		/// <summary>
		/// Initializes a new instance of the <see cref="ToolResult"/> class.
		/// </summary>
		public ToolResult()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ToolResult"/> class with the specified content.
		/// </summary>
		/// <param name="content">The main content of the tool result.</param>
		public ToolResult(string content)
		{
			Content = content ?? string.Empty;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ToolResult"/> class with the specified attachments.
		/// </summary>
		/// <param name="attachments">The attachments to include in the tool result.</param>
		public ToolResult(IEnumerable<IAttachment> attachments)
		{
			Content = string.Empty;
			if (attachments != null)
				_attachments.AddRange(attachments);
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="ToolResult"/> class with the specified attachments.
		/// </summary>
		/// <param name="attachments">The attachments to include in the tool result.</param>
		public ToolResult(params IAttachment[] attachments)
		{
			Content = string.Empty;
			if (attachments != null)
				_attachments.AddRange(attachments);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ToolResult"/> class with the specified content and attachments.
		/// </summary>
		/// <param name="content">The main content of the tool result.</param>
		/// <param name="attachments">The attachments to include in the tool result.</param>
		public ToolResult(string content, IEnumerable<IAttachment> attachments)
		{
			Content = content ?? string.Empty;
			if (attachments != null)
				_attachments.AddRange(attachments);
		}

		/// <summary>
		/// Adds an attachment to the tool result.
		/// </summary>
		/// <param name="attachment">The attachment to add.</param>
		public void AddAttachment(IAttachment attachment)
		{
			if (attachment == null)
				throw new ArgumentNullException(nameof(attachment));

			_attachments.Add(attachment);
		}

		/// <summary>
		/// Adds multiple attachments to the tool result.
		/// </summary>
		/// <param name="attachments">The attachments to add.</param>
		public void AddAttachments(IEnumerable<IAttachment> attachments)
		{
			if (attachments == null)
				throw new ArgumentNullException(nameof(attachments));

			_attachments.AddRange(attachments);
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