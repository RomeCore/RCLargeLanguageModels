using System;
using System.Collections.Generic;
using System.Text;
using RCLargeLanguageModels.Messages;

namespace RCLargeLanguageModels.Prompting.Templates
{
	/// <summary>
	/// The base class for all messages template nodes used for rendering prompts based on templates.
	/// </summary>
	public abstract class MessagesTemplateNode
	{
		/// <summary>
		/// Renders the template node based on the provided data.
		/// </summary>
		/// <param name="context">The context accessor containing the data to render.</param>
		/// <returns>A collection of messages representing the rendered template node.</returns>
		public abstract IEnumerable<IMessage> Render(TemplateContextAccessor context);
	}
}