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
		public ImmutableArray<Or<TokenPattern, int>> TokenPatternIds { get; }

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
			TokenPatternIds = tokenPatternIds?.Select(tp => new Or<TokenPattern, int>(tp)).ToImmutableArray()
				?? throw new ArgumentNullException(nameof(tokenPatternIds));
			if (TokenPatternIds.IsEmpty)
				throw new ArgumentException("At least one token pattern must be provided.", nameof(tokenPatternIds));
			ParsedValueFactory = parsedValueFactory ?? (pts => pts.Select(pt => pt.parsedValue).ToList());
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ChoiceTokenPattern"/> class.
		/// </summary>
		/// <param name="tokenPatterns">The token patterns to try.</param>
		/// <param name="parsedValueFactory">The factory method to create the parsed value from the matched tokens.</param>
		public SequenceTokenPattern(IEnumerable<TokenPattern> tokenPatterns, Func<List<ParsedToken>, object?>? parsedValueFactory = null)
		{
			TokenPatternIds = tokenPatterns?.Select(tp => new Or<TokenPattern, int>(tp)).ToImmutableArray()
				?? throw new ArgumentNullException(nameof(tokenPatterns));
			if (TokenPatternIds.IsEmpty)
				throw new ArgumentException("At least one token pattern must be provided.", nameof(tokenPatterns));
			ParsedValueFactory = parsedValueFactory ?? (pts => pts.Select(pt => pt.parsedValue).ToList());
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ChoiceTokenPattern"/> class.
		/// </summary>
		/// <param name="tokenPatterns">The token patterns to try.</param>
		/// <param name="parsedValueFactory">The factory method to create the parsed value from the matched tokens.</param>
		public SequenceTokenPattern(IEnumerable<Or<TokenPattern, int>> tokenPatterns, Func<List<ParsedToken>, object?>? parsedValueFactory = null)
		{
			TokenPatternIds = tokenPatterns?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(tokenPatterns));
			if (TokenPatternIds.IsEmpty)
				throw new ArgumentException("At least one token pattern must be provided.", nameof(tokenPatterns));
			ParsedValueFactory = parsedValueFactory ?? (pts => pts.Select(pt => pt.parsedValue).ToList());
		}

		public override bool TryMatch(int thisTokenId, ParserContext context, out ParsedToken token)
		{
			var currentContext = context.Copy();
			var totalLength = 0;
			var tokens = new List<ParsedToken>();

			foreach (var tokenId in TokenPatternIds)
			{
				ParsedToken subToken = ParsedToken.Fail;
				if (tokenId.VariantIndex == 0)
				{
					if (!tokenId.AsT1().TryMatch(-1, context, out subToken))
					{
						token = ParsedToken.Fail;
						return false;
					}
				}
				else if (tokenId.VariantIndex == 1)
				{
					if (!context.parser.TryMatchToken(tokenId.AsT2(), context, out subToken))
					{
						token = ParsedToken.Fail;
						return false;
					}
				}

				totalLength += subToken.length;
				tokens.Add(subToken);
				currentContext = currentContext.With(subToken.startIndex + subToken.length);
			}

			token = new ParsedToken(thisTokenId, context.position, totalLength, ParsedValueFactory.Invoke(tokens));
			return true;
		}
	}
}