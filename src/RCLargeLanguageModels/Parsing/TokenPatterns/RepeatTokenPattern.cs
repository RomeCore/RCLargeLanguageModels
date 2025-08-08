using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace RCLargeLanguageModels.Parsing.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that repeats a specific token multiple times.
	/// </summary>
	public class RepeatTokenPattern : TokenPattern
	{
		/// <summary>
		/// Gets the token pattern ID to repeat.
		/// </summary>
		public int TokenPattern { get; }

		/// <summary>
		/// Gets the minimum number of times the token pattern must repeat.
		/// </summary>
		public int MinCount { get; }

		/// <summary>
		/// Gets the maximum number of times the token pattern can repeat. -1 indicates no upper limit.
		/// </summary>
		public int MaxCount { get; }

		/// <summary>
		/// Gets the factory function that creates a parsed value from the matched tokens.
		/// </summary>
		public Func<List<ParsedToken>, object?> ParsedValueFactory { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="RepeatTokenPattern"/> class.
		/// </summary>
		/// <param name="tokenPatternId">The token pattern ID to repeat.</param>
		/// <param name="minCount">The minimum number of times the token pattern must repeat.</param>
		/// <param name="maxCount">The maximum number of times the token pattern can repeat.</param>
		/// <param name="parsedValueFactory">The factory function that creates a parsed value from the matched tokens.</param>
		public RepeatTokenPattern(int tokenPatternId, int minCount, int maxCount, Func<List<ParsedToken>, object?>? parsedValueFactory = null)
		{
			if (minCount < 0)
				throw new ArgumentOutOfRangeException(nameof(minCount), "minCount must be greater than or equal to 0");

			if (maxCount < minCount && maxCount >= 0)
				throw new ArgumentOutOfRangeException(nameof(maxCount), "maxCount must be greater than or equal to minCount or be negative if no maximum is specified.");

			TokenPattern = tokenPatternId;
			MinCount = minCount;
			MaxCount = Math.Max(maxCount, -1);
			ParsedValueFactory = parsedValueFactory ?? DefaultParsedValueFactory;
		}

		private static object? DefaultParsedValueFactory(List<ParsedToken> tokens) => tokens.Select(t => t.parsedValue).ToList();

		public override bool TryMatch(ParserContext context, out ParsedToken token)
		{
			var tokens = new List<ParsedToken>();
			var initialPosition = context.position;

			for (int i = 0; i < this.MaxCount || this.MaxCount == -1; i++)
			{
				ParsedToken matchedToken = ParsedToken.Fail;
				if (!context.parser.TryMatchToken(TokenPattern, context, out matchedToken))
				{
					break;
				}

				context.position = matchedToken.startIndex + matchedToken.length;
				tokens.Add(matchedToken);
			}

			if (tokens.Count < this.MinCount)
			{
				token = ParsedToken.Fail;
				return false;
			}

			token = new ParsedToken(
				Id,
				initialPosition,
				context.position - initialPosition,
				ParsedValueFactory(tokens));

			return true;
		}

		public override string ToString(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return $"repeat{{{MinCount}..{(MaxCount == -1 ? "" : MaxCount)}}}...";
			return $"repeat{{{MinCount}..{(MaxCount == -1 ? "" : MaxCount)}}}: " +
				$"{GetTokenPattern(TokenPattern).ToString(remainingDepth - 1)}";
		}

		public override bool Equals(object? obj)
		{
			return obj is RepeatTokenPattern pattern &&
				   TokenPattern == pattern.TokenPattern &&
				   MinCount == pattern.MinCount &&
				   MaxCount == pattern.MaxCount &&
				   ParsedValueFactory == pattern.ParsedValueFactory;
		}

		public override int GetHashCode()
		{
			int hashCode = -843078857;
			hashCode = hashCode * -1521134295 + TokenPattern.GetHashCode();
			hashCode = hashCode * -1521134295 + MinCount.GetHashCode();
			hashCode = hashCode * -1521134295 + MaxCount.GetHashCode();
			hashCode = hashCode * -1521134295 + ParsedValueFactory.GetHashCode();
			return hashCode;
		}
	}
}