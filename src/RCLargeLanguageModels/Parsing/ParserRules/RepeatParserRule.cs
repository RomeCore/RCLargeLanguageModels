using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace RCLargeLanguageModels.Parsing.ParserRules
{
	/// <summary>
	/// A parser rule that repeats a specified rule the specified range of times.
	/// </summary>
	public class RepeatParserRule : ParserRule
	{
		/// <summary>
		/// Gets the rule ID to repeat.
		/// </summary>
		public int Rule { get; }

		/// <summary>
		/// Gets the minimum number of times the rule must repeat.
		/// </summary>
		public int MinCount { get; }

		/// <summary>
		/// Gets the maximum number of times the rule can repeat. -1 indicates no upper limit.
		/// </summary>
		public int MaxCount { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="RepeatParserRule"/> class.
		/// </summary>
		/// <param name="ruleId">The token pattern ID to repeat.</param>
		/// <param name="minCount">The minimum number of times the token pattern must repeat.</param>
		/// <param name="maxCount">The maximum number of times the token pattern can repeat.</param>
		public RepeatParserRule(int ruleId, int minCount, int maxCount)
		{
			if (minCount < 0)
				throw new ArgumentOutOfRangeException(nameof(minCount), "minCount must be greater than or equal to 0");

			if (maxCount < minCount && maxCount >= 0)
				throw new ArgumentOutOfRangeException(nameof(maxCount), "maxCount must be greater than or equal to minCount or be negative if no maximum is specified.");

			Rule = ruleId;
			MinCount = minCount;
			MaxCount = Math.Max(maxCount, -1);
		}



		public override ParsedRule Parse(ParserContext context)
		{
			var childContext = AdvanceContext(ref context);

			var rules = new List<ParsedRule>();
			var initialPosition = childContext.position;

			for (int i = 0; i < this.MaxCount || this.MaxCount == -1; i++)
			{
				ParsedRule parsedRule = ParsedRule.Fail;
				if (!TryParseRule(Rule, childContext, out parsedRule))
				{
					break;
				}
				childContext.position = parsedRule.startIndex + parsedRule.length;
				parsedRule.occurency = i;
				rules.Add(parsedRule);
			}

			if (rules.Count < MinCount)
			{
				throw new ParsingException($"Expected at least {MinCount} repetitions of {GetRule(Rule)}\nbut found {rules.Count}.",
					childContext.str, initialPosition);
			}

			return new ParsedRule(
				Id,
				initialPosition,
				childContext.position - initialPosition,
				rules.ToImmutableList(),
				ParsedValueFactory);
		}

		public override bool TryParse(ParserContext context, out ParsedRule result)
		{
			var childContext = AdvanceContext(ref context);

			var rules = new List<ParsedRule>();
			var initialPosition = childContext.position;

			for (int i = 0; i < this.MaxCount || this.MaxCount == -1; i++)
			{
				ParsedRule parsedRule = ParsedRule.Fail;
				if (!TryParseRule(Rule, childContext, out parsedRule))
				{
					break;
				}
				childContext.position = parsedRule.startIndex + parsedRule.length;
				parsedRule.occurency = i;
				rules.Add(parsedRule);
			}

			if (rules.Count < MinCount)
			{
				childContext.errors.Add(new ParsingError(childContext.position,
					$"Expected at least {MinCount} repetitions of {GetRule(Rule)}\nbut found {rules.Count}."));
				result = ParsedRule.Fail;
				return false;
			}

			result = new ParsedRule(
				Id,
				initialPosition,
				childContext.position - initialPosition,
				rules.ToImmutableList(),
				ParsedValueFactory);

			return true;
		}



		public override string ToString(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return $"Repeat{{{MinCount}..{(MaxCount == -1 ? "" : MaxCount)}}}...";
			return $"Repeat{{{MinCount}..{(MaxCount == -1 ? "" : MaxCount)}}}: " +
				$"{GetRule(Rule).ToString(remainingDepth - 1)}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is RepeatParserRule rule &&
				   Rule == rule.Rule &&
				   MinCount == rule.MinCount &&
				   MaxCount == rule.MaxCount;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + Rule.GetHashCode();
			hashCode = hashCode * -1521134295 + MinCount.GetHashCode();
			hashCode = hashCode * -1521134295 + MaxCount.GetHashCode();
			return hashCode;
		}
	}
}