using System;
using System.Collections.Generic;
using System.Text;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Completions;

namespace RCLargeLanguageModels
{
	/// <summary>
	/// Represents a content holder that used for <see cref="IMessage"/> and <see cref="ICompletion"/>.
	/// </summary>
	public interface IContentHolder
	{
		/// <summary>
		/// Gets the text content.
		/// </summary>
		/// <remarks>
		/// For messages it excludes any other content, like attachments, tool calls and any other things. <br/>
		/// For completions it will contain the full completion content.
		/// </remarks>
		string? Content { get; }
	}
}