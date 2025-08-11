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
		/// Initializes a new instance of the <see cref="RepeatTokenPattern"/> class.
		/// </summary>
		/// <param name="tokenPatternId">The token pattern ID to repeat.</param>
		/// <param name="minCount">The minimum number of times the token pattern must repeat.</param>
		/// <param name="maxCount">The maximum number of times the token pattern can repeat.</param>
		public RepeatTokenPattern(int tokenPatternId, int minCount, int maxCount)
		{
			if (minCount < 0)
				throw new ArgumentOutOfRangeException(nameof(minCount), "minCount must be greater than or equal to 0");

			if (maxCount < minCount && maxCount >= 0)
				throw new ArgumentOutOfRangeException(nameof(maxCount), "maxCount must be greater than or equal to minCount or be negative if no maximum is specified.");

			TokenPattern = tokenPatternId;
			MinCount = minCount;
			MaxCount = Math.Max(maxCount, -1);
		}



		public override bool TryMatch(ParserContext context, out ParsedToken token)
		{
			var childContext = AdvanceContext(ref context);

			var tokens = new List<ParsedToken>();
			var initialPosition = context.position;

			for (int i = 0; i < MaxCount || MaxCount == -1; i++)
			{
				ParsedToken matchedToken = ParsedToken.Fail;
				if (!TryMatchToken(TokenPattern, childContext, out matchedToken)
					|| matchedToken.startIndex == childContext.position)
				{
					break;
				}

				childContext.position = matchedToken.startIndex + matchedToken.length;
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
				childContext.position - initialPosition,
				ParsedValueFactory);

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
			return base.Equals(obj) &&
				   obj is RepeatTokenPattern pattern &&
				   TokenPattern == pattern.TokenPattern &&
				   MinCount == pattern.MinCount &&
				   MaxCount == pattern.MaxCount;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + TokenPattern.GetHashCode();
			hashCode = hashCode * -1521134295 + MinCount.GetHashCode();
			hashCode = hashCode * -1521134295 + MaxCount.GetHashCode();
			return hashCode;
		}
	}
}