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
		/// Initializes a new instance of the <see cref="TokenParserRule"/> class.
		/// </summary>
		/// <param name="tokenPattern">The token pattern ID to match for this rule.</param>
		public TokenParserRule(int tokenPattern)
		{
			TokenPattern = tokenPattern;
		}



		public override bool TryParse(ParserContext context, ParserContext childContext, out ParsedRule result)
		{
			if (!TryMatchToken(TokenPattern, childContext, out var parsedToken))
			{
				context.RecordError($"Failed to parse token {GetTokenPattern(TokenPattern)}");
				result = ParsedRule.Fail;
				return false;
			}

			result = new ParsedRule(Id, parsedToken.startIndex, parsedToken.length, parsedToken,
				ParsedValueFactory ?? DefaultParsedValueFactory, parsedToken.intermediateValue);
			return true;
		}

		private static object? DefaultParsedValueFactory(ParsedRuleResult result) => result.Token.Value;



		public override string ToString(int remainingDepth)
		{
			return GetTokenPattern(TokenPattern).ToString(remainingDepth);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is TokenParserRule rule &&
				   TokenPattern == rule.TokenPattern;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + TokenPattern.GetHashCode();
			return hashCode;
		}
	}
}