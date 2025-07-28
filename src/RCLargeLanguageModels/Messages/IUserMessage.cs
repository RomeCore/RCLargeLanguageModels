using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Messages
{
	/// <summary>
	/// Represents a user message in chat with LLM.
	/// </summary>
	public interface IUserMessage : ISenderMessage, IAttachmentsMessage
	{
	}
}