using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using RCLargeLanguageModels.Parsing;
using RCLargeLanguageModels.Parsing.Building;
using RCLargeLanguageModels.Prompting.Templates.DataAccessors;

namespace RCLargeLanguageModels.Prompting.Templates
{
	public class LLTParser : ITemplateParser
	{
		private static readonly Parser _parser;

		static LLTParser()
		{
			var builder = new ParserBuilder();

			// Settings //

			builder.Settings()
				.Skip(s => s.Choice(
					c => c.Whitespaces(),
					c => c.Literal("@//").TextUntil('\n', '\r'), // @// C#-like comments
					c => c.Literal("@*").TextUntil("*@").Literal("*@")), // @*...*@ comments
					ParserSkippingStrategy.TryParseThenSkip) // Allows rules to capture skip-rules contents if can, such as whitespaces
				.CacheAll(); // If caching is disabled, prepare to wait for a long time (seconds) when encountering an error :P (you will also get a million of errors, seriously)

			// ---- Values ---- //

			builder.CreateToken("identifier")
				.Identifier();

			builder.CreateToken("methodName")
				.Identifier();

			builder.CreateToken("fieldName")
				.Identifier();

			builder.CreateToken("number")
				.Regex(@"\d+(?:\.\d+)?",
				m => new TemplateNumberAccessor(double.Parse(m.Text.Replace('.', ','))));

			builder.CreateToken("string")
				.Literal('"')
				.EscapedTextDoubleChars('"')
				.Literal('"')
				.Pass(v => v[1])
				.Transform(v => new TemplateStringAccessor(v.GetIntermediateValue<string>()));

			builder.CreateToken("boolean")
				.LiteralChoice(new []{ "true", "false" },
				m => new TemplateBooleanAccessor(m.GetIntermediateValue<string>() == "true"));

			builder.CreateToken("null")
				.Literal("null",
				_ => TemplateNullAccessor.Instance);

			// Constants //

			builder.CreateRule("constant")
				.Choice(
					c => c.Token("number"),
					c => c.Token("string"),
					c => c.Token("boolean"),
					c => c.Token("null"),
					c => c.Rule("constant_array"),
					c => c.Rule("constant_object"));

			builder.CreateRule("constant_pair")
				.Token("identifier")
				.Literal(":")
				.Rule("constant");

			builder.CreateRule("constant_array")
				.Literal("[")
				.ZeroOrMoreSeparated(b => b.Rule("constant"), b => b.Literal(","), allowTrailingSeparator: true)
				.Literal("]");

			builder.CreateRule("constant_object")
				.Literal("{")
				.ZeroOrMoreSeparated(b => b.Rule("constant_pair"), b => b.Literal(","), allowTrailingSeparator: true)
				.Literal("}");

			// Expression values //

			builder.CreateRule("primary")
				.Choice(
					c => c.Token("number"),
					c => c.Token("string"),
					c => c.Token("boolean"),
					c => c.Token("null"),
					c => c.Token("identifier"),
					c => c.Rule("array"),
					c => c.Rule("object"),
					c => c.Literal('(').Rule("expression").Literal(')'));

			builder.CreateRule("value")
				.Rule("primary"); // Add alias for 'primary' rule

			builder.CreateRule("pair")
				.Token("identifier")
				.Literal(':')
				.Rule("value");

			builder.CreateRule("array")
				.Literal("[")
				.ZeroOrMoreSeparated(b => b.Rule("value"), b => b.Literal(","), allowTrailingSeparator: true)
				.Literal("]");

			builder.CreateRule("object")
				.Literal("{")
				.ZeroOrMoreSeparated(b => b.Rule("pair"), b => b.Literal(","), allowTrailingSeparator: true)
				.Literal("}");

			// Expressions //

			builder.CreateRule("postfix_member")
				.Rule("primary")
				.ZeroOrMore(b => b.Choice(
					b => b.Literal('.') // Method call
						  .Token("methodName")
						  .Literal('(')
						  .ZeroOrMoreSeparated(a => a.Rule("expression"), s => s.Literal(','))
						  .Literal(')'),

					b => b.Literal('.') // Field access
						  .Token("fieldName"),

					b => b.Literal('[') // Index access
						  .Rule("expression")
						  .Literal(']')
					));

			builder.CreateRule("prefix_operator")
				.ZeroOrMore(b => b.LiteralChoice("+", "-", "!"))
				.Rule("postfix_member");

			// Operators //

			builder.CreateRule("multiplicative_operator") // multiplicative
				.OneOrMoreSeparated(b => b.Rule("prefix_operator"),
					o => o.LiteralChoice("*", "/", "%"),
					includeSeparatorsInResult: true);

			builder.CreateRule("additive_operator") // additive
				.OneOrMoreSeparated(b => b.Rule("multiplicative_operator"),
					o => o.LiteralChoice("+", "-"),
					includeSeparatorsInResult: true);

			builder.CreateRule("relational_operator") // relational (<, <=, >, >=)
				.OneOrMoreSeparated(b => b.Rule("additive_operator"),
					o => o.LiteralChoice("<", "<=", ">", ">="),
					includeSeparatorsInResult: true);

			builder.CreateRule("equality_operator") // equality (==, !=)
				.OneOrMoreSeparated(b => b.Rule("relational_operator"),
					o => o.LiteralChoice("==", "!="),
					includeSeparatorsInResult: true);

			builder.CreateRule("logical_and_operator") // logical AND
				.OneOrMoreSeparated(b => b.Rule("equality_operator"),
					o => o.Literal("&&"),
					includeSeparatorsInResult: true);

			builder.CreateRule("logical_or_operator") // logical OR
				.OneOrMoreSeparated(b => b.Rule("logical_and_operator"),
					o => o.Literal("||"),
					includeSeparatorsInResult: true,
					config: c => c.SkippingStrategy(ParserSkippingStrategy.SkipBeforeParsing));

			// Final expression = lowest precedence

			builder.CreateRule("expression")
				.Rule("logical_or_operator");

			// ---- Templates ---- //

			builder.CreateRule("template")
				.Choice(
					b => b.Rule("text_template"),
					b => b.Rule("messages_template"));

			builder.CreateRule("metadata_block")
				.Literal('@')
				.Literal("metadata")
				.Rule("constant_object");

			// Messages templates //

			builder.CreateRule("messages_template")
				.Literal('@')
				.Literal("messages")
				.Whitespaces()
				.Literal("template")
				.Whitespaces()
				.Token("identifier")
				.Rule("messages_template_main_block");

			builder.CreateRule("messages_template_main_block")
				.Literal('{')
				.Optional(b =>
					b.Rule("metadata_block"))
				.Rule("message_statements")
				.Literal('}');

			builder.CreateRule("messages_template_block")
				.Literal('{')
				.Rule("message_statements")
				.Literal('}');

			builder.CreateRule("message_statements")
				.ZeroOrMore(b => b
					.Literal('@')
					.Choice(
						c => c.Rule("message_block"),
						c => c.Rule("messages_if"),
						c => c.Rule("messages_foreach"),
						c => c.Rule("messages_while")));

			builder.CreateRule("message_block")
				.Choice(
					b => b.Rule("message_block_explicit_role"),
					b => b.Rule("message_block_variable_role"));

			builder.CreateRule("message_block_explicit_role")
				.LiteralChoice("system", "user", "assistant", "tool")
				.Whitespaces()
				.Literal("message")
				.Rule("text_template_block");

			builder.CreateRule("message_block_variable_role")
				.Literal("message")
				.Rule("message_variable_role_block");

			builder.CreateRule("message_variable_role_block")
				.Literal('{')
				.Literal('@')
				.Literal("role")
				.Rule("text_expression")
				.Rule("text_statements")
				.Literal('}');

			builder.CreateRule("messages_if")
				.Literal("if")
				.Whitespaces()
				.Rule("expression")
				.Rule("messages_template_block")
				.Optional(
					b => b.Literal("else").Choice(
						b => b.Rule("messages_if"),
						b => b.Rule("messages_template_block")));

			builder.CreateRule("messages_foreach")
				.Literal("foreach")
				.Whitespaces()
				.Token("identifier")
				.Literal("in")
				.Whitespaces()
				.Rule("expression")
				.Rule("messages_template_block");

			builder.CreateRule("messages_while")
				.Literal("while")
				.Rule("expression")
				.Rule("messages_template_block");

			// Text templates //

			builder.CreateRule("text_template")
				.Literal('@')
				.Literal("template")
				.Whitespaces()
				.Token("identifier")
				.Rule("text_template_main_block");

			builder.CreateRule("text_content")
				.EscapedTextDoubleChars("@{}", allowsEmpty: false);

			builder.CreateRule("text_statements")
				.ZeroOrMore(b => b.Choice(
					c => c.Rule("text_content"),
					c => c.Rule("text_statement")));

			builder.CreateRule("text_template_main_block")
				.Literal('{')
				.Optional(b => b.Rule("metadata_block"))
				.Rule("text_statements")
				.Literal('}');

			builder.CreateRule("text_template_block")
				.Literal('{')
				.Rule("text_statements")
				.Literal('}');

			builder.CreateRule("text_statement")
				.Literal('@')
				.Choice(
					b => b.Rule("text_if"),
					b => b.Rule("text_foreach"),
					b => b.Rule("text_while"),
					b => b.Rule("text_expression")
					);

			builder.CreateRule("text_expression")
				.Rule("prefix_operator"); // We don't want to use binary expressions in text statements

			builder.CreateRule("text_if")
				.Literal("if")
				.Whitespaces()
				.Rule("expression")
				.Rule("text_template_block")
				.Optional(
					b => b.Literal("else").Choice(
						b => b.Rule("text_if"),
						b => b.Rule("text_template_block")));

			builder.CreateRule("text_foreach")
				.Literal("foreach")
				.Whitespaces()
				.Token("identifier")
				.Literal("in")
				.Whitespaces()
				.Rule("expression")
				.Rule("text_template_block");

			builder.CreateRule("text_while")
				.Literal("while")
				.Rule("expression")
				.Rule("text_template_block");

			// Main rule

			builder.CreateRule("file_content")
				.ZeroOrMore(b => b.Rule("template"))
				.EOF()
				.Transform(v => v.Children[0].Value);

			_parser = builder.Build();
		}

		public ParsedRuleResult ParseAST(string templateString)
		{
			return _parser.ParseRule("file_content", templateString).Optimized(ParseTreeOptimization.Default);
		}

		public IEnumerable<ITemplate> Parse(string templateString)
		{
			return _parser.ParseRule("file_content", templateString).Value as IEnumerable<ITemplate>;
		}
	}
}