using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Prompting.Templates
{
	/// <summary>
	/// Represents a prompt template for generating prompts.
	/// </summary>
	public class PromptTemplate
	{
		private PromptTemplateNode _node;

		/// <summary>
		/// Renders the prompt using the provided data accessor.
		/// </summary>
		/// <param name="context">The context accessor to use for rendering.</param>
		/// <returns>The rendered prompt as a string.</returns>
		public string Render(TemplateContextAccessor context)
		{
			return _node.Render(context);
		}
	}
}