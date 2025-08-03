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
		public ImmutableArray<Or<TokenPattern, int>> TokenPatternIds { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChoiceTokenPattern"/> class.
		/// </summary>
		/// <param name="tokenPatternIds">The token patterns ids to try.</param>
		public ChoiceTokenPattern(IEnumerable<int> tokenPatternIds)
		{
			TokenPatternIds = tokenPatternIds?.Select(tp => new Or<TokenPattern, int>(tp)).ToImmutableArray()
				?? throw new ArgumentNullException(nameof(tokenPatternIds));
			if (TokenPatternIds.IsEmpty)
				throw new ArgumentException("At least one token pattern must be provided.", nameof(tokenPatternIds));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ChoiceTokenPattern"/> class.
		/// </summary>
		/// <param name="tokenPatterns">The token patterns to try.</param>
		public ChoiceTokenPattern(IEnumerable<TokenPattern> tokenPatterns)
		{
			TokenPatternIds = tokenPatterns?.Select(tp => new Or<TokenPattern, int>(tp)).ToImmutableArray()
				?? throw new ArgumentNullException(nameof(tokenPatterns));
			if (TokenPatternIds.IsEmpty)
				throw new ArgumentException("At least one token pattern must be provided.", nameof(tokenPatterns));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ChoiceTokenPattern"/> class.
		/// </summary>
		/// <param name="tokenPatterns">The token patterns to try.</param>
		public ChoiceTokenPattern(IEnumerable<Or<TokenPattern, int>> tokenPatterns)
		{
			TokenPatternIds = tokenPatterns?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(tokenPatterns));
			if (TokenPatternIds.IsEmpty)
				throw new ArgumentException("At least one token pattern must be provided.", nameof(tokenPatterns));
		}

		public override bool TryMatch(int thisTokenId, ParserContext context, out ParsedToken token)
		{
			foreach (var tokenId in TokenPatternIds)
			{
				if (tokenId.VariantIndex == 0)
				{
					if (tokenId.AsT1().TryMatch(-1, context, out token))
					{
						token = new ParsedToken(thisTokenId, token.startIndex, token.length, token.parsedValue);
						return true;
					}
				}
				else if (tokenId.VariantIndex == 1)
				{
					if (context.parser.TryMatchToken(tokenId.AsT2(), context, out token))
					{
						token = new ParsedToken(thisTokenId, token.startIndex, token.length, token.parsedValue);
						return true;
					}
				}
			}

			token = ParsedToken.Fail;
			return false;
		}
	}
}