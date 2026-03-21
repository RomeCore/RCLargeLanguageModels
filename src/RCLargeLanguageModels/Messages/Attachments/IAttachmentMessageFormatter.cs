using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Messages.Attachments
{
	public interface IAttachmentMessageFormatter
	{
		/// <summary>
		/// Formats the specified content and textual attachments into a single string.
		/// </summary>
		/// <param name="content">The content to format.</param>
		/// <param name="attachments">The textual attachments to include in the formatted string.</param>
		/// <returns>A formatted string containing the specified content and textual attachments.</returns>
		string Format(string content, IEnumerable<ITextAttachment> attachments);
	}

	/// <summary>
	/// Represents a default implementation of the <see cref="IAttachmentMessageFormatter"/> interface.
	/// </summary>
	public class DefaultAttachmentMessageFormatter : IAttachmentMessageFormatter
	{
		public string Format(string content, IEnumerable<ITextAttachment> attachments)
		{
			StringBuilder sb = new StringBuilder();

			foreach (var attachment in attachments)
			{
				sb.Append(attachment.Title).AppendLine(":");
				sb.AppendLine(attachment.GetContent()).AppendLine();
			}

			if (sb.Length > 0)
			{
				if (!string.IsNullOrWhiteSpace(content))
				{
					sb.AppendLine("Content:");
					sb.Append(content);
				}
				return sb.ToString();
			}

			return content;
		}
	}
}