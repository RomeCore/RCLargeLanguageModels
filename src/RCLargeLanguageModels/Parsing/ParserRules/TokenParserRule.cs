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



		public override bool TryParse(ParserContext context, out ParsedRule result)
		{
			var childContext = AdvanceContext(ref context);

			if (!TryMatchToken(TokenPattern, childContext, out var parsedToken))
			{
				context.RecordError($"Failed to parse {GetTokenPattern(TokenPattern)}");
				result = ParsedRule.Fail;
				return false;
			}

			result = new ParsedRule(Id, parsedToken.startIndex, parsedToken.length, parsedToken,
				ParsedValueFactory, parsedToken.intermediateValue);
			return true;
		}

		public override ParsedRule Parse(ParserContext context)
		{
			var childContext = AdvanceContext(ref context);

			if (TryMatchToken(TokenPattern, childContext, out var parsedToken))
			{
				return new ParsedRule(Id, parsedToken.startIndex, parsedToken.length, parsedToken,
					ParsedValueFactory, parsedToken.intermediateValue);
			}

			throw new ParsingException($"Failed to parse '{GetTokenPattern(TokenPattern)}'",
				context.str, context.position);
		}



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