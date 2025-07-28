using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using RCLargeLanguageModels.Messages.Attachments;

namespace RCLargeLanguageModels.Messages
{
	/// <summary>
	/// Represents a message in chat with LLM that have attachments.
	/// </summary>
	public interface IAttachmentsMessage : IMessage
	{
		/// <summary>
		/// Gets the attachments list of the message.
		/// </summary>
		IReadOnlyList<IAttachment> Attachments { get; }
	}

	/// <summary>
	/// A class that contains extension methods for <see cref="IAttachmentsMessage"/> interfaces.
	/// </summary>
	public static class AttachmentsMessageExtension
	{
		/// <summary>
		/// Builds the content of the message with only text attachments included.
		/// </summary>
		/// <param name="message">The message to build the content for.</param>
		/// <returns>
		/// If the message has any text attachments, returns the content of the message with text attachments included.
		/// Otherwise, just returns the content of the message
		/// </returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static string BuildContentWithTextAttachments(this IAttachmentsMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (!message.Attachments.Any(a => a is ITextAttachment ta && !string.IsNullOrEmpty(ta.Content)))
				return message.Content;

			StringBuilder result = new StringBuilder();
			result.AppendLine(message.Content);
			result.AppendLine();
			result.AppendLine("ATTACHMENTS:"); // Uppercased text is strangely affects the LLM's response.
			result.AppendLine();

			foreach (var attachment in message.Attachments)
			{
				if (attachment is ITextAttachment textAttachment)
				{
					if (string.IsNullOrEmpty(textAttachment.Content))
						continue;

					result.AppendLine($"{textAttachment.Title}:");
					result.AppendLine(textAttachment.Content);
					result.AppendLine();
				}
			}

			// Remove the last two characters (the last 2 line breaks)
			result.Length -= 2;

			return result.ToString();
		}

		/// <summary>
		/// Builds the content of the message with attachments converted to text.
		/// </summary>
		/// <param name="message">The message to build the content for.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>
		/// The <see cref="Task"/> that represents the asynchronous operation.
		/// The task result contains the content of the message with attachments converted to text.
		/// </returns>
		/// <remarks>
		/// Uses <see cref="AttachmentToTextConverter"/> to convert attachments to text.
		/// </remarks>
		public static async Task<string> BuildContentWithToTextConversion(this IAttachmentsMessage message, CancellationToken cancellationToken = default)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (!message.Attachments.Any())
				return message.Content;

			StringBuilder result = new StringBuilder();
			result.AppendLine(message.Content);
			result.AppendLine();
			result.AppendLine("ATTACHMENTS:");
			result.AppendLine();

			foreach (var attachment in message.Attachments)
			{
				string text = await AttachmentToTextConverter
					.ConvertAsync(attachment, cancellationToken).ConfigureAwait(false);

				if (string.IsNullOrEmpty(text))
					continue;

				result.AppendLine($"{attachment.Title}:");
				result.AppendLine(text);
				result.AppendLine();
			}

			// Remove the last two characters (the last 2 line breaks)
			result.Length -= 2;

			return result.ToString();
		}
	}
}