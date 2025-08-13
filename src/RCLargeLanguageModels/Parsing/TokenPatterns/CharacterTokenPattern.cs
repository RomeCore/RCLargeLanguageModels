using System;

namespace RCLargeLanguageModels.Parsing.TokenPatterns
{
	/// <summary>
	/// The token pattern that matches one character based on a predicate function.
	/// </summary>
	public class CharacterTokenPattern : TokenPattern
	{
		/// <summary>
		/// Gets the predicate that determines whether a character is part of this pattern.
		/// </summary>
		public Func<char, bool> CharacterPredicate { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="CharacterTokenPattern"/> class.
		/// </summary>
		/// <param name="characterPredicate">The predicate that determines whether a character is part of this pattern.</param>
		public CharacterTokenPattern(Func<char, bool> characterPredicate)
		{
			CharacterPredicate = characterPredicate ?? throw new ArgumentNullException(nameof(characterPredicate));
		}

		public override bool TryMatch(ParserContext context, ParserContext childContext, out ParsedToken token)
		{
			if (context.position < context.str.Length && CharacterPredicate(context.str[context.position]))
			{
				token = new ParsedToken(Id, context.position, 1);
				return true;
			}

			token = ParsedToken.Fail;
			return false;
		}

		public override string ToString(int remainingDepth)
		{
			return "predicate";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is CharacterTokenPattern other &&
				   CharacterPredicate == other.CharacterPredicate;
		}

		public override int GetHashCode()
		{
			int hash = base.GetHashCode();
			hash *= CharacterPredicate.GetHashCode() * 17;
			return hash;
		}
	}
}