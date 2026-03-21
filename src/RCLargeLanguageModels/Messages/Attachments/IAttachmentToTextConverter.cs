using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Messages.Attachments
{
	/// <summary>
	/// Converts an attachment to a <see cref="ITextAttachment"/>.
	/// </summary>
	public interface IAttachmentToTextConverter
	{
		/// <summary>
		/// Determines whether this converter can convert the specified attachment.
		/// </summary>
		/// <param name="attachment">The attachment to check.</param>
		/// <returns><see langword="true"/> if this converter can convert the specified attachment; otherwise, <see langword="false"></see></returns>
		bool CanConvert(IAttachment attachment);

		/// <summary>
		/// Converts an attachment to a text .
		/// </summary>
		/// <param name="attachment">The attachment to convert.</param>
		/// <returns>A string representing the attachment.</returns>
		Task<ITextAttachment> ConvertAsync(IAttachment attachment);
	}
}