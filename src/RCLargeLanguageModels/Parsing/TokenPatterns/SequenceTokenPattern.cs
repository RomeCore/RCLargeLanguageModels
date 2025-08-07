using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace RCLargeLanguageModels.Parsing.TokenPatterns
{
	/// <summary>
	/// Matches a sequence of token patterns in order.
	/// </summary>
	public class SequenceTokenPattern : TokenPattern
	{
		/// <summary>
		/// The IDs of the token patterns to match in sequence.
		/// </summary>
		public ImmutableArray<int> TokenPatterns { get; }

		/// <summary>
		/// The factory method to create the parsed value from the matched tokens.
		/// </summary>
		public Func<List<ParsedToken>, object?> ParsedValueFactory { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChoiceTokenPattern"/> class.
		/// </summary>
		/// <param name="tokenPatternIds">The token patterns ids to try.</param>
		/// <param name="parsedValueFactory">The factory method to create the parsed value from the matched tokens.</param>
		public SequenceTokenPattern(IEnumerable<int> tokenPatternIds, Func<List<ParsedToken>, object?>? parsedValueFactory = null)
		{
			TokenPatterns = tokenPatternIds?.ToImmutableArray()
				?? throw new ArgumentNullException(nameof(tokenPatternIds));
			if (TokenPatterns.IsEmpty)
				throw new ArgumentException("At least one token pattern must be provided.", nameof(tokenPatternIds));
			ParsedValueFactory = parsedValueFactory ?? DefaultParsedValueFactory;
		}

		private static object? DefaultParsedValueFactory(List<ParsedToken> tokens) => tokens.Select(t => t.parsedValue).ToList();

		public override bool TryMatch(int thisTokenId, ParserContext context, out ParsedToken token)
		{
			var totalLength = 0;
			var tokens = new List<ParsedToken>();

			foreach (var tokenId in TokenPatterns)
			{
				if (!context.parser.TryMatchToken(tokenId, context, out var subToken))
				{
					token = ParsedToken.Fail;
					return false;
				}

				totalLength += subToken.length;
				tokens.Add(subToken);
				context.position = subToken.startIndex + subToken.length;
			}

			token = new ParsedToken(thisTokenId, context.position, totalLength, ParsedValueFactory.Invoke(tokens));
			return true;
		}

		public override bool Equals(object? obj)
		{
			return obj is SequenceTokenPattern pattern &&
				   TokenPatterns.SequenceEqual(pattern.TokenPatterns) &&
				   ParsedValueFactory == pattern.ParsedValueFactory;
		}

		public override int GetHashCode()
		{
			int hashCode = 736527562;
			hashCode = hashCode * -1521134295 + TokenPatterns.GetSequenceHashCode();
			hashCode = hashCode * -1521134295 + ParsedValueFactory.GetHashCode();
			return hashCode;
		}
	}
}