using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

namespace RCLargeLanguageModels.Parsing.ParserRules
{
	/// <summary>
	/// Represents a parser rule that is optional.
	/// </summary>
	public class OptionalParserRule : ParserRule
	{
		/// <summary>
		/// Gets the rule ID that is optional.
		/// </summary>
		public int Rule { get; }

		/// <summary>
		/// Gets the factory method to create a parsed value from the matched token.
		/// </summary>
		public Func<ParsedRule?, object?> ParsedValueFactory { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OptionalParserRule"/> class.
		/// </summary>
		/// <param name="rule">The rule ID that is optional.</param>
		/// <param name="parsedValueFactory">The factory method to create a parsed value from the matched token.</param>
		public OptionalParserRule(int rule, Func<ParsedRule?, object?>? parsedValueFactory = null)
		{
			Rule = rule;
			ParsedValueFactory = parsedValueFactory ?? DefaultParsedValueFactory;
		}

		private static object? DefaultParsedValueFactory(ParsedRule? r) => r?.parsedValue;

		public override bool TryParse(ParserContext context, out ParsedRule result)
		{
			var str = ToString(4);
			if (context.parser.TryParseRule(Rule, context, out var parsedRule))
			{
				result = new ParsedRule(Id, parsedRule.startIndex, parsedRule.length, ImmutableList.Create(parsedRule), ParsedValueFactory(parsedRule));
				return true;
			}
			else
			{
				result = new ParsedRule(Id, context.position, 0, ImmutableList<ParsedRule>.Empty, ParsedValueFactory(null));
				return true;
			}
		}

		public override ParsedRule Parse(ParserContext context)
		{
			if (context.parser.TryParseRule(Rule, context, out var parsedRule))
			{
				return new ParsedRule(Id, parsedRule.startIndex, parsedRule.length, ImmutableList.Create(parsedRule), ParsedValueFactory(parsedRule));
			}
			else
			{
				return new ParsedRule(Id, context.position, 0, ImmutableList<ParsedRule>.Empty, ParsedValueFactory(null));
			}
		}

		public override string ToString(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return "Optional...";
			return $"Optional: {GetRule(Rule).ToString(remainingDepth - 1)}";
		}

		public override bool Equals(object? obj)
		{
			return obj is OptionalParserRule rule &&
				   Rule == rule.Rule &&
				   ParsedValueFactory == rule.ParsedValueFactory;
		}

		public override int GetHashCode()
		{
			int hashCode = 1613406236;
			hashCode = hashCode * -1521134295 + Rule.GetHashCode();
			hashCode = hashCode * -1521134295 + ParsedValueFactory.GetHashCode();
			return hashCode;
		}
	}
}