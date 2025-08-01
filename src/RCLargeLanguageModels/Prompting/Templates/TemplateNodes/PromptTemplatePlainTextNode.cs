using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates.TemplateNodes
{
	/// <summary>
	/// Represents a plain text node in a prompt template.
	/// </summary>
	public class PromptTemplatePlainTextNode : PromptTemplateNode
	{
		/// <summary>
		/// The text content of the node.
		/// </summary>
		public string Text { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="PromptTemplatePlainTextNode"/> class.
		/// </summary>
		/// <param name="text">The plain text content.</param>
		public PromptTemplatePlainTextNode(string text)
		{
			Text = text;
		}

		public override string Render(TemplateContextAccessor context)
		{
			return Text;
		}
	}
}