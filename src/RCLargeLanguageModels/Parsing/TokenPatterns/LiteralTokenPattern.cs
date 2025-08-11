using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Parsing.TokenPatterns
{
	/// <summary>
	/// Matches a literal string in the input text.
	/// </summary>
	/// Passes a matched original literal <see cref="string"/> (not captured) as an intermediate value.
	/// For example, if pattern was "HELLO" with case-insensitive comparison,
	/// then the intermediate value would be "HELLO", not "hello".
	public class LiteralTokenPattern : TokenPattern
	{
		/// <summary>
		/// The literal string to match.
		/// </summary>
		public string Literal { get; }

		/// <summary>
		/// Gets the comparer used for matching.
		/// </summary>
		public StringComparer Comparer { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LiteralTokenPattern"/> class.
		/// </summary>
		/// <param name="literal">The literal string to match.</param>
		/// <param name="comparer">The comparer to use for matching.</param>
		public LiteralTokenPattern(string literal, StringComparer? comparer = null)
		{
			Literal = string.IsNullOrEmpty(literal)
				? throw new ArgumentException("Literal cannot be null or empty.", nameof(literal))
				: literal;
			Comparer = comparer ?? StringComparer.Ordinal;
		}



		public override bool TryMatch(ParserContext context, ParserContext childContext, out ParsedToken token)
		{
			if (context.position + Literal.Length > context.str.Length)
			{
				context.RecordError($"Failed to match {this}, input text too short.");
				token = ParsedToken.Fail;
				return false;
			}

			var inputSlice = context.str.Substring(context.position, Literal.Length);
			if (Comparer.Compare(inputSlice, Literal) == 0)
			{
				token = new ParsedToken(Id, context.position, Literal.Length, ParsedValueFactory, Literal);
				return true;
			}

			context.RecordError($"Failed to match {this}.");
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
				   obj is LiteralTokenPattern pattern &&
				   Literal == pattern.Literal &&
				   Comparer == pattern.Comparer;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + Literal.GetHashCode();
			hashCode = hashCode * -1521134295 + Comparer.GetHashCode();
			return hashCode;
		}
	}
}