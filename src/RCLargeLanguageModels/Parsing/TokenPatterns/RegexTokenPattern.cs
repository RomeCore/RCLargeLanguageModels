using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace RCLargeLanguageModels.Parsing.TokenPatterns
{
	/// <summary>
	/// Matches a regular expression pattern in the input text.
	/// </summary>
	public class RegexTokenPattern : TokenPattern
	{
		/// <summary>
		/// The regular expression pattern string to match.
		/// </summary>
		public string RegexPattern { get; }
		
		/// <summary>
		/// The regular expression to match.
		/// </summary>
		public Regex Regex { get; }

		/// <summary>
		/// Gets the factory function for creating parsed values from matches.
		/// </summary>
		public Func<Match, object?> ParsedValueFactory { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="RegexTokenPattern"/> class.
		/// </summary>
		/// <param name="pattern">The regular expression pattern.</param>
		/// <param name="parsedValueFactory">The factory function for creating parsed values from matches.</param>
		/// <param name="options">The regex options (default is None).</param>
		public RegexTokenPattern(string pattern, Func<Match, object?>? parsedValueFactory = null, RegexOptions options = RegexOptions.Compiled)
		{
			if (string.IsNullOrEmpty(pattern))
				throw new ArgumentException("Pattern cannot be null or empty.", nameof(pattern));
			RegexPattern = pattern.TrimStart('^');
			Regex = new Regex($"^{RegexPattern}", options);
			ParsedValueFactory = parsedValueFactory ?? DefaultParsedValueFactory;
		}

		private static object? DefaultParsedValueFactory(Match m) => m.Value;

		public override bool TryMatch(int thisTokenId, ParserContext context, out ParsedToken token)
		{
			var match = Regex.Match(context.str, context.position);
			if (!match.Success || match.Index != context.position)
			{
				token = ParsedToken.Fail;
				return false;
			}

			token = new ParsedToken(thisTokenId, context.position, match.Length, ParsedValueFactory.Invoke(match));
			return true;
		}

		public override bool Equals(object? obj)
		{
			return obj is RegexTokenPattern pattern &&
				   RegexPattern == pattern.RegexPattern &&
				   ParsedValueFactory == pattern.ParsedValueFactory;
		}

		public override int GetHashCode()
		{
			int hashCode = 1824601363;
			hashCode = hashCode * -1521134295 + RegexPattern.GetHashCode();
			hashCode = hashCode * -1521134295 + ParsedValueFactory.GetHashCode();
			return hashCode;
		}
	}
}