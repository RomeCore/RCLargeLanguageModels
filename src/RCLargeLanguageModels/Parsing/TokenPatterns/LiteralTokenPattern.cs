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
		/// Gets the factory function that creates a parsed value from the matched literal string.
		/// </summary>
		public Func<string, object?> ParsedValueFactory { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LiteralTokenPattern"/> class.
		/// </summary>
		/// <remarks>
		/// Defaults to case-sensitive ordinal comparison.
		/// </remarks>
		/// <param name="literal">The literal string to match.</param>
		/// <param name="parsedValueFactory">The factory function that creates a parsed value from the matched literal string.</param>
		public LiteralTokenPattern(string literal, Func<string, object?>? parsedValueFactory = null)
		{
			Literal = string.IsNullOrEmpty(literal) ? throw new ArgumentException("Literal cannot be null or empty.", nameof(literal)) : literal;
			Comparer = StringComparer.Ordinal;
			ParsedValueFactory = parsedValueFactory ?? DefaultParsedValueFactory;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LiteralTokenPattern"/> class.
		/// </summary>
		/// <param name="literal">The literal string to match.</param>
		/// <param name="comparer">The comparer to use for matching.</param>
		/// <param name="parsedValueFactory">The factory function that creates a parsed value from the matched literal string.</param>
		public LiteralTokenPattern(string literal, StringComparer comparer, Func<string, object?>? parsedValueFactory = null)
		{
			Literal = string.IsNullOrEmpty(literal) ? throw new ArgumentException("Literal cannot be null or empty.", nameof(literal)) : literal;
			Comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
			ParsedValueFactory = parsedValueFactory ?? DefaultParsedValueFactory;
		}

		private static object? DefaultParsedValueFactory(string s) => s;

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
				token = new ParsedToken(thisTokenId, context.position, Literal.Length, ParsedValueFactory.Invoke(inputSlice));
				return true;
			}

			token = ParsedToken.Fail;
			return false;
		}

		public override string ToString(ParserContext context)
		{
			return $"literal: '{Literal}'";
		}

		public override bool Equals(object? obj)
		{
			return obj is LiteralTokenPattern pattern &&
				   Literal == pattern.Literal &&
				   Comparer == pattern.Comparer &&
				   ParsedValueFactory == pattern.ParsedValueFactory;
		}

		public override int GetHashCode()
		{
			int hashCode = 2015609103;
			hashCode = hashCode * -1521134295 + Literal.GetHashCode();
			hashCode = hashCode * -1521134295 + Comparer.GetHashCode();
			hashCode = hashCode * -1521134295 + ParsedValueFactory.GetHashCode();
			return hashCode;
		}
	}
}