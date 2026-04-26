using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Messages.Attachments
{
	/// <summary>
	/// Represents an attachment that contains an image.
	/// </summary>
	public interface IImageAttachment : IAttachment
	{
		/// <summary>
		/// The format of the image (e.g. 'jpeg', 'png', ...).
		/// </summary>
		string Format { get; }

		/// <summary>
		/// Gets the base64 encoded image data.
		/// </summary>
		/// <returns>The base64 encoded image data.</returns>
		string GetBase64();
	}
}