using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates
{
	/// <summary>
	/// Represents a parser for templates.
	/// </summary>
	public interface ITemplateParser
	{
		/// <summary>
		/// Parses a prompt template string into a <see cref="ITemplate"/> objects.
		/// </summary>
		/// <param name="templateString">The prompt template string to parse.</param>
		/// <returns>A collection of <see cref="ITemplate"/> objects representing the parsed templates.</returns>
		IEnumerable<ITemplate> Parse(string templateString);
	}
}