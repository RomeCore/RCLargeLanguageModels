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
	/// Implementations should not retrive data at the construction, data should be stored to be used in serialization (for example,
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

	/// <summary>
	/// Represents an attachment that contains text.
	/// </summary>
	public interface ITextAttachment : IAttachment
	{
		/// <summary>
		/// Gets the text content of the attachment.
		/// </summary>
		string Content { get; }
	}

	/// <summary>
	/// Represents an attachment that have an URI source.
	/// </summary>
	public interface IUriAttachment : IAttachment
	{
		/// <summary>
		/// Gets the URI of the attachment.
		/// </summary>
		string Uri { get; }
	}

	/// <summary>
	/// The factory class for creating attachments.
	/// </summary>
	public static class Attachment
	{
		/// <summary>
		/// Creates a new file attachment using the specified file path.
		/// </summary>
		/// <param name="filepath">The path to the file (can be absolute or relative).</param>
		/// <returns>The created attachment.</returns>
		/// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
		public static IAttachment CreateRawFile(string filepath)
		{
			var file = new FileInfo(filepath);
			if (!file.Exists)
				throw new FileNotFoundException("File not found", filepath);
			return new RawTextAttachment($"FILE {file.Name}", filepath);
		}
	}
}