using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates
{
	/// <summary>
	/// Represents a node in the prompt template hierarchy.
	/// </summary>
	public abstract class PromptTemplateNode
	{
		/// <summary>
		/// Renders the prompt template node using the provided data accessor.
		/// </summary>
		/// <param name="dataAccessor">The data accessor to use for rendering.</param>
		/// <returns>The rendered prompt template as a string.</returns>
		public abstract string Render(TemplateDataAccessor dataAccessor);
	}
}