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
					c => c.Literal("@//").TextUntil('\n', '\r'),
					c => c.Literal("@*").TextUntil("*@").Literal("*@")));

			builder.CreateRule("expression")
				.Identifier();

			builder.CreateRule("text_content")
				.EscapedTextDoubleChars("@{}", allowsEmpty: false);

			builder.CreateRule("template")
				.Choice(
					b => b.Rule("text_template"));

			builder.CreateRule("text_template")
				.Literal('@')
				.Literal("template")
				.Identifier()
				.Rule("text_template_content");

			builder.CreateRule("text_template_content")
				.Literal('{')
				.ZeroOrMore(b => b.Choice(new Action<RuleBuilder>[]{
					c => c.Rule("text_content"),
					c => c.Rule("text_statement")}, config: c => c.NoSkipping(ParserSettingMode.LocalForSelfOnly)))
				.Literal('}');

			builder.CreateRule("text_statement")
				.Literal('@')
				.Choice(
					b => b.Rule("text_if"),
					b => b.Rule("text_foreach"),
					b => b.Rule("text_while")
					);

			builder.CreateRule("text_if")
				.Literal("if")
				.Rule("expression")
				.Rule("text_template_content")
				.Optional(
					b => b.Literal("else").Rule("text_if"));

			builder.CreateRule("text_foreach")
				.Literal("foreach")
				.Identifier()
				.Literal("in")
				.Rule("expression")
				.Rule("text_template_content");

			builder.CreateRule("text_while")
				.Literal("while")
				.Rule("expression")
				.Rule("text_template_content");

			builder.CreateRule("file_content")
				.ZeroOrMore(b => b.Rule("template"))
				.EOF();

			_parser = builder.Build();
		}

		public ParsedRuleResult ParseAST(string templateString)
		{
			return _parser.ParseRule("file_content", templateString);
		}

		public IEnumerable<ITemplate> Parse(string templateString)
		{
			return _parser.ParseRule("file_content", templateString).Value as IEnumerable<ITemplate>;
		}
	}
}