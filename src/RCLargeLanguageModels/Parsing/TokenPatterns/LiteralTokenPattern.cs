using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Parsing.TokenPatterns
{
	/// <summary>
	/// Matches a literal string in the input text.
	/// </summary>
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
		/// Gets the parsed value to put in the token.
		/// </summary>
		public object? ParsedValue { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LiteralTokenPattern"/> class.
		/// </summary>
		/// <remarks>
		/// Defaults to case-sensitive ordinal comparison.
		/// </remarks>
		/// <param name="literal">The literal string to match.</param>
		public LiteralTokenPattern(string literal)
		{
			Literal = string.IsNullOrEmpty(literal) ? throw new ArgumentException("Literal cannot be null or empty.", nameof(literal)) : literal;
			Comparer = StringComparer.Ordinal;
			ParsedValue = null;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LiteralTokenPattern"/> class.
		/// </summary>
		/// <param name="literal">The literal string to match.</param>
		/// <param name="comparer">The comparer to use for matching.</param>
		public LiteralTokenPattern(string literal, StringComparer comparer)
		{
			Literal = string.IsNullOrEmpty(literal) ? throw new ArgumentException("Literal cannot be null or empty.", nameof(literal)) : literal;
			Comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
			ParsedValue = null;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LiteralTokenPattern"/> class.
		/// </summary>
		/// <param name="literal">The literal string to match.</param>
		/// <param name="comparer">The comparer to use for matching.</param>
		/// <param name="parsedValue">The value to put in the token.</param>
		public LiteralTokenPattern(string literal, StringComparer comparer, object? parsedValue)
		{
			Literal = literal ?? throw new ArgumentNullException(nameof(literal));
			Comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
			ParsedValue = parsedValue;
		}

		public override bool TryMatch(int thisTokenId, ParserContext context, out ParsedToken token)
		{
			if (context.position + Literal.Length > context.str.Length)
			{
				token = ParsedToken.Fail;
				return false;
			}

			var inputSlice = context.str.Substring(context.position, Literal.Length);
			if (Comparer.Compare(inputSlice, Literal) == 0)
			{
				token = ParsedToken.Fail;
				return false;
			}

			token = new ParsedToken(thisTokenId, context.position, Literal.Length, ParsedValue);
			return true;
		}
	}
}