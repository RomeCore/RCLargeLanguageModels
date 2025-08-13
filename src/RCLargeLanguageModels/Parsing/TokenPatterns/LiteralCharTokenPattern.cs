using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Parsing.TokenPatterns
{
	/// <summary>
	/// Matches a literal character in the input text.
	/// </summary>
	/// Passes a matched original literal <see cref="char"/> (not captured) as an intermediate value.
	/// For example, if pattern was 'H' with case-insensitive comparison,
	/// then the intermediate value would be 'H', not 'h'.
	public class LiteralCharTokenPattern : TokenPattern
	{
		readonly char[] charPool = new char[1];

		/// <summary>
		/// The literal character to match.
		/// </summary>
		public char Literal { get; }

		/// <summary>
		/// Gets the string comparison type used for literal matching.
		/// </summary>
		public StringComparison Comparison { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LiteralCharTokenPattern"/> class.
		/// </summary>
		/// <param name="literal">The literal character to match.</param>
		/// <param name="comparison">The string comparison type to use for literal matching.</param>
		public LiteralCharTokenPattern(char literal, StringComparison comparison = StringComparison.Ordinal)
		{
			Literal = literal;
			Comparison = comparison;
			charPool[0] = literal;
		}



		public override bool TryMatch(ParserContext context, ParserContext childContext, out ParsedToken token)
		{
			if (context.position + 1 > context.str.Length)
			{
				token = ParsedToken.Fail;
				return false;
			}

			if (Comparison  == StringComparison.Ordinal)
			{
				if (Literal == context.str[context.position])
				{
					token = new ParsedToken(Id, context.position, 1, Literal);
					return true;
				}
			}
			else
			{
				if (context.str.AsSpan(context.position, 1).Equals(charPool.AsSpan(), Comparison))
				{
					token = new ParsedToken(Id, context.position, 1, Literal);
					return true;
				}
			}

			token = ParsedToken.Fail;
			return false;
		}



		public override string ToString(int remainingDepth)
		{
			return $"literal: '{Literal}'";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is LiteralCharTokenPattern pattern &&
				   Literal == pattern.Literal &&
				   Comparison == pattern.Comparison;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + Literal.GetHashCode();
			hashCode = hashCode * -1521134295 + Comparison.GetHashCode();
			return hashCode;
		}
	}
}