using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Messages.Attachments;
using RCLargeLanguageModels.Metadata;
using RCLargeLanguageModels.Tools;

namespace RCLargeLanguageModels.Messages
{
	/// <summary>
	/// Represents a message in chat with LLM.
	/// </summary>
	/// <remarks>
	/// See also: <para/>
	/// <see cref="SystemMessage"/> <br/>
	/// <see cref="UserMessage"/> <br/>
	/// <see cref="AssistantMessage"/> <br/>
	/// <see cref="PartialAssistantMessage"/> <br/>
	/// <see cref="ToolMessage"/>
	/// </remarks>
	public interface IMessage : IContentHolder
	{
		/// <summary>
		/// Gets the message role.
		/// </summary>
		Role Role { get; }
	}
}