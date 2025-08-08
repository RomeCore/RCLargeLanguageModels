using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Parsing
{
	/// <summary>
	/// Represents a parser rule that can be used to parse input strings.
	/// </summary>
	public abstract class ParserRule : ParserElement
	{
		/// <summary>
		/// Tries to parse the input string using this rule.
		/// </summary>
		/// <param name="context">The parser context containing the input string and other relevant information.</param>
		/// <param name="result">The parsed rule if parsing is successful; otherwise, <see langword="null"/>.</param>
		/// <returns><see langword="true"/> if the input string can be successfully parsed using this rule; otherwise, <see langword="false"/>.</returns>
		public abstract bool TryParse(ParserContext context, out ParsedRule result);

		/// <summary>
		/// Parses the input string using this rule. If parsing fails, an exception is thrown.
		/// </summary>
		/// <param name="context">The parser context containing the input string and other relevant information.</param>
		/// <returns>The parsed rule.</returns>
		public abstract ParsedRule Parse(ParserContext context);
	}
}