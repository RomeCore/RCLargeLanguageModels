using System;
using System.Collections.Generic;
using System.Text;
using RCLargeLanguageModels.Parsing;
using RCLargeLanguageModels.Parsing.Building;

namespace RCLargeLanguageModels.Prompting.Templates
{
	public class LLTParser : ITemplateParser
	{
		private static readonly Parser _parser;

		static LLTParser()
		{
			var builder = new ParserBuilder();

			builder.Settings()
				.Skip(s => s.Choice(
					c => c.Whitespaces(),
					c => c.Literal("@//").ZeroOrMoreChars(c => c != '\n' && c != '\r')));


		}

		public IEnumerable<ITemplate> Parse(string templateString)
		{
			return _parser.ParseRule("content", templateString).Value as IEnumerable<ITemplate>;
		}
	}
}