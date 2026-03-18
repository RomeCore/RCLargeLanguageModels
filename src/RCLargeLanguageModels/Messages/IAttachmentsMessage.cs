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
}