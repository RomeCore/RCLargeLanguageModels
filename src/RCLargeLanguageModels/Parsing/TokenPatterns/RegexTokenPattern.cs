using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace RCLargeLanguageModels.Parsing.TokenPatterns
{
	/// <summary>
	/// Matches a regular expression pattern in the input text.
	/// </summary>
	/// <remarks>
	/// Passes a <see cref="Match"/> object from the regex match as an intermediate value.
	/// </remarks>
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
		/// Initializes a new instance of the <see cref="RegexTokenPattern"/> class.
		/// </summary>
		/// <param name="pattern">The regular expression pattern.</param>
		/// <param name="options">The regex options (default is None).</param>
		public RegexTokenPattern(string pattern, RegexOptions options = RegexOptions.Compiled)
		{
			if (string.IsNullOrEmpty(pattern))
				throw new ArgumentException("Pattern cannot be null or empty.", nameof(pattern));
			RegexPattern = pattern;
			Regex = new Regex($"\\G{RegexPattern}", options);
		}



		public override bool TryMatch(ParserContext context, ParserContext childContext, out ParsedToken token)
		{
			var match = Regex.Match(context.str, context.position);
			if (!match.Success || match.Index != context.position)
			{
				token = ParsedToken.Fail;
				return false;
			}

			token = new ParsedToken(Id, context.position, match.Length, match);
			return true;
		}



		public override string ToString(int remainingDepth)
		{
			return $"regex: '{RegexPattern}'";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is RegexTokenPattern pattern &&
				   RegexPattern == pattern.RegexPattern;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + RegexPattern.GetHashCode();
			return hashCode;
		}
	}
}