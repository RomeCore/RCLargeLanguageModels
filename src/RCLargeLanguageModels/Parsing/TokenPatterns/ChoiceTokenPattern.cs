using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace RCLargeLanguageModels.Parsing.TokenPatterns
{
	/// <summary>
	/// Matches one of several token patterns.
	/// </summary>
	public class ChoiceTokenPattern : TokenPattern
	{
		/// <summary>
		/// The IDs of the token patterns to try.
		/// </summary>
		public ImmutableArray<int> Choices { get; }

		/// <summary>
		/// Gets the factory function that creates a parsed value from the matched token.
		/// </summary>
		public Func<ParsedToken, object?> ParsedValueFactory { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChoiceTokenPattern"/> class.
		/// </summary>
		/// <param name="tokenPatternIds">The token patterns ids to try.</param>
		/// <param name="parsedValueFactory">The factory function that creates a parsed value from the matched token.</param>
		public ChoiceTokenPattern(IEnumerable<int> tokenPatternIds, Func<ParsedToken, object?> parsedValueFactory = null)
		{
			Choices = tokenPatternIds?.ToImmutableArray()
				?? throw new ArgumentNullException(nameof(tokenPatternIds));
			if (Choices.IsEmpty)
				throw new ArgumentException("At least one token pattern must be provided.", nameof(tokenPatternIds));
			ParsedValueFactory = parsedValueFactory ?? DefaultParsedValueFactory;
		}

		private static object? DefaultParsedValueFactory(ParsedToken r) => r.parsedValue;

		public override bool TryMatch(ParserContext context, out ParsedToken token)
		{
			foreach (var tokenId in Choices)
			{
				if (context.parser.TryMatchToken(tokenId, context, out token))
				{
					token = new ParsedToken(Id, token.startIndex, token.length, token.parsedValue);
					return true;
				}
			}

			token = ParsedToken.Fail;
			return false;
		}

		public override string ToString(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return "choice...";
			return $"choice:\n" +
				string.Join("\n", Choices.Select(c => GetTokenPattern(c).ToString(remainingDepth - 1)))
				.Indent("  ");
		}

		public override bool Equals(object? obj)
		{
			return obj is ChoiceTokenPattern other &&
				   Choices.SequenceEqual(other.Choices) &&
				   ParsedValueFactory == other.ParsedValueFactory;
		}

		public override int GetHashCode()
		{
			int hashCode = 1613406236;
			hashCode = hashCode * -1521134295 + Choices.GetSequenceHashCode();
			hashCode = hashCode * -1521134295 + ParsedValueFactory.GetHashCode();
			return hashCode;
		}
	}
}