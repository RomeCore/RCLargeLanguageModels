using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Messages.Attachments
{
	/// <summary>
	/// Represents an message attachment.
	/// </summary>
	/// <remarks>
	/// Implementations should not retrive data after the construction, data should be stored to be used in serialization (for example,
	/// file attachment should read file from disk when it being constructed).
	/// </remarks>
	public interface IAttachment
	{
		/// <summary>
		/// Gets the type of the attachment.
		/// </summary>
		AttachmentType Type { get; }

		/// <summary>
		/// Gets the title of attachment.
		/// </summary>
		/// <remarks>
		/// Typically, this is the name of the file without its full path.
		/// </remarks>
		string Title { get; }
	}
}