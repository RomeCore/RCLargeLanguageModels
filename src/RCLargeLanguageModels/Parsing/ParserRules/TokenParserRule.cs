using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RCLargeLanguageModels.Parsing.ParserRules
{
	/// <summary>
	/// Represents a parser rule that matches a specific token.
	/// </summary>
	public class TokenParserRule : ParserRule
	{
		/// <summary>
		/// The token pattern ID to match for this rule.
		/// </summary>
		public int TokenPattern { get; }

		/// <summary>
		/// The factory method to create a parsed value from the matched token.
		/// </summary>
		public Func<ParsedToken, object?> ParsedValueFactory { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TokenParserRule"/> class.
		/// </summary>
		/// <param name="tokenPattern">The token pattern ID to match for this rule.</param>
		/// <param name="parsedValueFactory">The factory method to create a parsed value from the matched token.</param>
		public TokenParserRule(int tokenPattern, Func<ParsedToken, object?>? parsedValueFactory = null)
		{
			TokenPattern = tokenPattern;
			ParsedValueFactory = parsedValueFactory ?? DefaultParsedValueFactory;
		}

		private static object? DefaultParsedValueFactory(ParsedToken token) => token.parsedValue;

		public override bool TryParse(int thisRuleId, ParserContext context, out ParsedRule result)
		{
			ParsedToken parsedToken = ParsedToken.Fail;

			if (!context.parser.TryMatchToken(TokenPattern, context, out parsedToken))
			{
				context.errors.Add(new ParsingError(context.position, $"Expected token '{context.parser.TokenPatterns[TokenPattern]}'"));
				result = ParsedRule.Fail;
				return false;
			}

			result = new ParsedRule(thisRuleId, context.position, parsedToken.length, parsedToken, ParsedValueFactory(parsedToken));
			return true;
		}

		public override ParsedRule Parse(int thisRuleId, ParserContext context)
		{
			if (context.parser.TryMatchToken(TokenPattern, context, out var parsedToken))
			{
				return new ParsedRule(thisRuleId, context.position, parsedToken.length, parsedToken, ParsedValueFactory(parsedToken));
			}

			throw new ParsingException($"Expected token '{context.parser.TokenPatterns[TokenPattern]}'", context.str, context.position);
		}

		public override bool Equals(object? obj)
		{
			return obj is TokenParserRule rule &&
				   TokenPattern == rule.TokenPattern &&
				   ParsedValueFactory == rule.ParsedValueFactory;
		}

		public override int GetHashCode()
		{
			int hashCode = 813679753;
			hashCode = hashCode * -1521134295 + TokenPattern.GetHashCode();
			hashCode = hashCode * -1521134295 + ParsedValueFactory.GetHashCode();
			return hashCode;
		}
	}
}