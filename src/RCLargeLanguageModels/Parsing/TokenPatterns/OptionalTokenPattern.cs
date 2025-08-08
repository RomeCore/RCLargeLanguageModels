using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace RCLargeLanguageModels.Parsing.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that is optional. It can match either the wrapped token pattern or no tokens at all.
	/// </summary>
	public class OptionalTokenPattern : TokenPattern
	{
		/// <summary>
		/// The token pattern ID that this optional pattern wraps.
		/// </summary>
		public int TokenPattern { get; }

		/// <summary>
		/// The factory function that creates a parsed value for the matched token.
		/// </summary>
		public Func<ParsedToken?, object?> ParsedValueFactory { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OptionalTokenPattern"/> class.
		/// </summary>
		/// <param name="tokenPatternId">The token pattern ID that this optional pattern wraps.</param>
		/// <param name="parsedValueFactory">The factory function that creates a parsed value for the matched token.</param>
		public OptionalTokenPattern(int tokenPatternId, Func<ParsedToken?, object?>? parsedValueFactory = null)
		{
			TokenPattern = tokenPatternId;
			ParsedValueFactory = parsedValueFactory ?? DefaultParsedValueFactory;
		}

		private static object? DefaultParsedValueFactory(ParsedToken? c) => c.GetValueOrDefault();

		public override bool TryMatch(ParserContext context, out ParsedToken token)
		{
			if (context.parser.TryMatchToken(TokenPattern, context, out var matchedToken))
			{
				token = new ParsedToken(Id, matchedToken.startIndex, matchedToken.length, ParsedValueFactory(matchedToken));
				return true;
			}
			else
			{
				token = new ParsedToken(Id, context.position, 0, ParsedValueFactory(null));
				return true;
			}
		}

		public override string ToString(int remainingDepth = 2)
		{
			if (remainingDepth <= 0)
				return "optional...";
			return $"optional: {GetTokenPattern(TokenPattern).ToString(remainingDepth - 1)}";
		}

		public override bool Equals(object? obj)
		{
			return obj is OptionalTokenPattern pattern &&
				   TokenPattern == pattern.TokenPattern &&
				   ParsedValueFactory == pattern.ParsedValueFactory;
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