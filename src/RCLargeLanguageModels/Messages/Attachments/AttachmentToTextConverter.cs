using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS1998

namespace RCLargeLanguageModels.Messages.Attachments
{
	/// <summary>
	/// The utility class to convert attachments to text.
	/// </summary>
	public static class AttachmentToTextConverter
	{
		/// <summary>
		/// Converts the attachment to text asynchronously.
		/// </summary>
		/// <param name="attachment">The attachment to convert.</param>
		/// <param name="cancellationToken">The cancellation token that used to cancel the operation.</param>
		/// <returns>The text representation of the attachment.</returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="NotSupportedException"></exception>
		public static async Task<string> ConvertAsync(IAttachment attachment, CancellationToken cancellationToken = default)
		{
			if (attachment == null)
				throw new ArgumentNullException(nameof(attachment));

			if (attachment is ITextAttachment textAttachment)
				return textAttachment.Content;

			throw new NotSupportedException($"Attachment of type {attachment.Type} is not supported.");
		}
	}
}