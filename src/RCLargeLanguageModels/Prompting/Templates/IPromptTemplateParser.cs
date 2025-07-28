using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates
{
	/// <summary>
	/// Represents a parser for prompt templates.
	/// </summary>
	public interface IPromptTemplateParser
	{
		/// <summary>
		/// Parses a prompt template string into a <see cref="PromptTemplate"/> object.
		/// </summary>
		/// <param name="templateString">The prompt template string to parse.</param>
		/// <returns>A <see cref="PromptTemplate"/> object representing the parsed template.</returns>
		PromptTemplate Parse(string templateString);
	}
}