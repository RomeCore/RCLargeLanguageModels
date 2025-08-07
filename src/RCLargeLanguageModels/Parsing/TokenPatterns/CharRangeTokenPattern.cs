using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

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
		/// <param name="parsedValueFactory">The factory function that creates a parsed value for a matched character.</param>
		public CharRangeTokenPattern(char minInclusiveChar, char maxInclusiveChar, Func<char, object?>? parsedValueFactory = null)
		{
			MinChar = minInclusiveChar;
			MaxChar = maxInclusiveChar;
			ParsedValueFactory = parsedValueFactory ?? DefaultParsedValueFactory;
		}

		private static object? DefaultParsedValueFactory(char c) => c;

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

		public override string ToString(ParserContext context)
		{
			return $"[{MinChar}-{MaxChar}]";
		}

		public override bool Equals(object? obj)
		{
			return obj is CharRangeTokenPattern pattern &&
				   MinChar == pattern.MinChar &&
				   MaxChar == pattern.MaxChar &&
				   ParsedValueFactory == pattern.ParsedValueFactory;
		}

		public override int GetHashCode()
		{
			int hashCode = 516612889;
			hashCode = hashCode * -1521134295 + MinChar.GetHashCode();
			hashCode = hashCode * -1521134295 + MaxChar.GetHashCode();
			hashCode = hashCode * -1521134295 + ParsedValueFactory.GetHashCode();
			return hashCode;
		}
	}
}