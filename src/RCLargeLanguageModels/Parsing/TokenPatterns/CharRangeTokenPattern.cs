using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Parsing.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that matches a range of characters.
	/// </summary>
	public class CharRangeTokenPattern : TokenPattern
	{
		/// <summary>
		/// Gets the minimum inclusive character that can be matched by this pattern.
		/// </summary>
		public char MinChar { get; }

		/// <summary>
		/// Gets the maximum inclusive character that can be matched by this pattern.
		/// </summary>
		public char MaxChar { get; }

		/// <summary>
		/// Gets the factory function that creates a parsed value for a matched character.
		/// </summary>
		public Func<char, object?> ParsedValueFactory { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CharRangeTokenPattern"/> class.
		/// </summary>
		/// <param name="minInclusiveChar">The minimum inclusive character that can be matched by this pattern.</param>
		/// <param name="maxInclusiveChar">The maximum inclusive character that can be matched by this pattern.</param>
		public CharRangeTokenPattern(char minInclusiveChar, char maxInclusiveChar)
		{
			MinChar = minInclusiveChar;
			MaxChar = maxInclusiveChar;
		}

		public override bool TryMatch(int thisTokenId, ParserContext context, out ParsedToken token)
		{
			if (context.position >= context.str.Length)
			{
				token = ParsedToken.Fail;
				return false;
			}

			char currentChar = context.str[context.position];
			if (currentChar >= MinChar && currentChar <= MaxChar)
			{
				token = new ParsedToken(thisTokenId, context.position, 1, ParsedValueFactory(currentChar));
				return true;
			}
			else
			{
				token = ParsedToken.Fail;
				return false;
			}
		}
	}
}