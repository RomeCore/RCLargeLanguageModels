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
		/// Initializes a new instance of the <see cref="ChoiceTokenPattern"/> class.
		/// </summary>
		/// <param name="tokenPatternIds">The token patterns ids to try.</param>
		public SequenceTokenPattern(IEnumerable<int> tokenPatternIds)
		{
			TokenPatterns = tokenPatternIds?.ToImmutableArray()
				?? throw new ArgumentNullException(nameof(tokenPatternIds));
			if (TokenPatterns.IsEmpty)
				throw new ArgumentException("At least one token pattern must be provided.", nameof(tokenPatternIds));
		}



		public override bool TryMatch(ParserContext context, out ParsedToken token)
		{
			var childContext = AdvanceContext(ref context);

			var initialPosition = childContext.position;
			var tokens = new List<ParsedToken>();

			foreach (var tokenId in TokenPatterns)
			{
				if (!TryMatchToken(tokenId, childContext, out var subToken))
				{
					token = ParsedToken.Fail;
					return false;
				}

				tokens.Add(subToken);
				childContext.position = subToken.startIndex + subToken.length;
			}

			token = new ParsedToken(Id, initialPosition, context.position - initialPosition, ParsedValueFactory);
			return true;
		}



		public override string ToString(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return "sequence...";
			return $"sequence:\n" +
				string.Join("\n", TokenPatterns.Select(c => GetTokenPattern(c).ToString(remainingDepth - 1)))
				.Indent("  ");
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is SequenceTokenPattern pattern &&
				   TokenPatterns.SequenceEqual(pattern.TokenPatterns);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + TokenPatterns.GetSequenceHashCode();
			return hashCode;
		}
	}
}