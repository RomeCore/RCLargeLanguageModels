using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Messages.Attachments
{
	/// <summary>
	/// Represents a raw text attachment.
	/// </summary>
	public class RawTextAttachment : ITextAttachment
	{
		public string Title { get; }
		public string Content { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="RawTextAttachment"/> class.
		/// </summary>
		/// <param name="title">The title of the attachment.</param>
		/// <param name="content">The content of the attachment.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="title"/> is null.</exception>
		public RawTextAttachment(string title, string content)
		{
			Title = title ?? throw new ArgumentNullException(nameof(title));
			Content = content ?? string.Empty;
		}

		public string GetContent()
		{
			return Content;
		}
	}
}