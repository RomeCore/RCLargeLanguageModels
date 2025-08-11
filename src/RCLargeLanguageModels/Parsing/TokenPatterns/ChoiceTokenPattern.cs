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
		/// Initializes a new instance of the <see cref="ChoiceTokenPattern"/> class.
		/// </summary>
		/// <param name="tokenPatternIds">The token patterns ids to try.</param>
		public ChoiceTokenPattern(IEnumerable<int> tokenPatternIds)
		{
			Choices = tokenPatternIds?.ToImmutableArray()
				?? throw new ArgumentNullException(nameof(tokenPatternIds));
			if (Choices.IsEmpty)
				throw new ArgumentException("At least one token pattern must be provided.", nameof(tokenPatternIds));
		}



		public override bool TryMatch(ParserContext context, out ParsedToken token)
		{
			var childContext = AdvanceContext(ref context);

			foreach (var tokenId in Choices)
			{
				if (TryMatchToken(tokenId, childContext, out token))
				{
					token = new ParsedToken(Id, token.startIndex, token.length,
						ParsedValueFactory, token.intermediateValue);
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
			return base.Equals(obj) &&
				   obj is ChoiceTokenPattern other &&
				   Choices.SequenceEqual(other.Choices);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + Choices.GetSequenceHashCode();
			return hashCode;
		}
	}
}