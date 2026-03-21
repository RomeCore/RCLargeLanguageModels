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
		/// Gets the base64 encoded image data.
		/// </summary>
		/// <returns>The base64 encoded image data.</returns>
		public string GetBase64();
	}
}