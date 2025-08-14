using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Parsing.TokenPatterns
{
	/// <summary>
	/// The token pattern that matches one or more characters based on a predicate function.
	/// </summary>
	public class RepeatCharactersTokenPattern : TokenPattern
	{
		/// <summary>
		/// Gets the predicate that determines whether a character is part of this pattern.
		/// </summary>
		public Func<char, bool> CharacterPredicate { get; }

		/// <summary>
		/// The minimum number of characters to match (inclusive).
		/// </summary>
		public int MinCount { get; }

		/// <summary>
		/// The maximum number of characters to match (inclusive). -1 indicates no upper limit.
		/// </summary>
		public int MaxCount { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="RepeatCharactersTokenPattern"/> class.
		/// </summary>
		/// <param name="characterPredicate">The predicate that determines whether a character is part of this pattern.</param>
		/// <param name="minCount">The minimum number of characters to match (inclusive).</param>
		/// <param name="maxCount">The maximum number of characters to match (inclusive). -1 indicates no upper limit.</param>
		public RepeatCharactersTokenPattern(Func<char, bool> characterPredicate, int minCount, int maxCount)
		{
			if (minCount < 0)
				throw new ArgumentOutOfRangeException(nameof(minCount), "minCount must be greater than or equal to 0");

			if (maxCount < minCount && maxCount != -1)
				throw new ArgumentOutOfRangeException(nameof(maxCount), "maxCount must be greater than or equal to minCount or be -1 if no maximum is specified.");

			MinCount = minCount;
			MaxCount = maxCount;

			CharacterPredicate = characterPredicate ?? throw new ArgumentNullException(nameof(characterPredicate));
		}

		public override bool TryMatch(ParserContext context, ParserContext childContext, out ParsedToken token)
		{
			int initialPosition = context.position;
			while (context.position < context.str.Length &&
				(MaxCount == -1 || context.position - initialPosition < MaxCount) &&
				CharacterPredicate(context.str[context.position]))
				context.position++;

			int count = context.position - initialPosition;
			if (count < MinCount)
			{
				token = ParsedToken.Fail;
				return false;
			}

			token = new ParsedToken(Id, initialPosition, count);
			return true;
		}

		public override string ToString(int remainingDepth)
		{
			return $"repeat predicate{{{MinCount}..{(MaxCount == -1 ? "" : MaxCount)}}}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is RepeatCharactersTokenPattern other &&
				   MinCount == other.MinCount &&
				   MaxCount == other.MaxCount &&
				   CharacterPredicate == other.CharacterPredicate;
		}

		public override int GetHashCode()
		{
			int hash = base.GetHashCode();
			hash *= MinCount.GetHashCode() * 17 + 397;
			hash *= MaxCount.GetHashCode() * 17 + 397;
			hash *= CharacterPredicate.GetHashCode() * 17 + 397;
			return hash;
		}
	}
}