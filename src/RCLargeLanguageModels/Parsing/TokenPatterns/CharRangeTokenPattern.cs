using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace RCLargeLanguageModels.Parsing.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that matches a range of characters.
	/// </summary>
	/// <remarks>
	/// Passes a <see cref="char"/> as an intermediate value.
	/// </remarks>
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
		/// Initializes a new instance of the <see cref="CharRangeTokenPattern"/> class.
		/// </summary>
		/// <param name="minInclusiveChar">The minimum inclusive character that can be matched by this pattern.</param>
		/// <param name="maxInclusiveChar">The maximum inclusive character that can be matched by this pattern.</param>
		public CharRangeTokenPattern(char minInclusiveChar, char maxInclusiveChar)
		{
			MinChar = minInclusiveChar;
			MaxChar = maxInclusiveChar;
		}



		public override bool TryMatch(ParserContext context, out ParsedToken token)
		{
			AdvanceContext(ref context);

			if (context.position >= context.str.Length)
			{
				token = ParsedToken.Fail;
				return false;
			}

			char currentChar = context.str[context.position];
			if (currentChar >= MinChar && currentChar <= MaxChar)
			{
				token = new ParsedToken(Id, context.position, 1, ParsedValueFactory, currentChar);
				return true;
			}
			else
			{
				token = ParsedToken.Fail;
				return false;
			}
		}



		public override string ToString(int remainingDepth)
		{
			return $"[{MinChar}-{MaxChar}]";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is CharRangeTokenPattern pattern &&
				   MinChar == pattern.MinChar &&
				   MaxChar == pattern.MaxChar;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + MinChar.GetHashCode();
			hashCode = hashCode * -1521134295 + MaxChar.GetHashCode();
			return hashCode;
		}
	}
}