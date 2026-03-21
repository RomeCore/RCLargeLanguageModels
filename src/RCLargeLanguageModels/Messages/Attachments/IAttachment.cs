using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Messages.Attachments
{
	/// <summary>
	/// Represents an attachment that can be included in a message.
	/// </summary>
	public interface IAttachment
	{
		/// <summary>
		/// Gets the title of attachment, used for display purposes.
		/// </summary>
		string Title { get; }
	}
}