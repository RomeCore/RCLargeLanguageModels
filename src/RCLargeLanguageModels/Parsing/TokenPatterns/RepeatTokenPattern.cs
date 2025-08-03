using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Parsing.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that repeats a specific token multiple times.
	/// </summary>
	public class RepeatTokenPattern : TokenPattern
	{
		/// <summary>
		/// The token pattern or its id to repeat.
		/// </summary>
		public Or<TokenPattern, int> TokenPattern { get; }

		/// <summary>
		/// The minimum number of times the token should be repeated.
		/// </summary>
		public int MinCount { get; }

		/// <summary>
		/// The maximum number of times the token should be repeated. If set to -1, there is no upper limit.
		/// </summary>
		public int MaxCount { get; }

		/// <summary>
		/// The factory method to create the parsed value from a list of parsed tokens.
		/// </summary>
		public Func<List<ParsedToken>, object?> ParsedValueFactory { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="RepeatTokenPattern"/> class.
		/// </summary>
		/// <param name="tokenPattern">The token pattern or its id to repeat.</param>
		/// <param name="minCount">The minimum number of times the token should be repeated.</param>
		/// <param name="maxCount">The maximum number of times the token should be repeated. If set to -1, there is no upper limit.</param>
		/// <exception cref="ArgumentException"></exception>
		public RepeatTokenPattern(Or<TokenPattern, int> tokenPattern, int minCount, int maxCount = -1)
		{
			TokenPattern = tokenPattern;
			MinCount = minCount;
			MaxCount = maxCount;

			if (minCount < 0 || (minCount > maxCount && maxCount >= 0))
				throw new ArgumentException($"Invalid repeat count values. MinCount: {MinCount}, MaxCount: {MaxCount}.");
		}

		public override bool TryMatch(int thisTokenId, ParserContext context, out ParsedToken token)
		{
			token = ParsedToken.Fail;
			return false;
		}
	}
}