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
		/// Gets the factory function that creates a parsed value from the matched rules.
		/// </summary>
		public Func<List<ParsedRule>, object?> ParsedValueFactory { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="RepeatParserRule"/> class.
		/// </summary>
		/// <param name="ruleId">The token pattern ID to repeat.</param>
		/// <param name="minCount">The minimum number of times the token pattern must repeat.</param>
		/// <param name="maxCount">The maximum number of times the token pattern can repeat.</param>
		/// <param name="parsedValueFactory">The factory function that creates a parsed value from the matched tokens.</param>
		public RepeatParserRule(int ruleId, int minCount, int maxCount, Func<List<ParsedRule>, object?>? parsedValueFactory = null)
		{
			if (minCount < 0)
				throw new ArgumentOutOfRangeException(nameof(minCount), "minCount must be greater than or equal to 0");

			if (maxCount < minCount && maxCount >= 0)
				throw new ArgumentOutOfRangeException(nameof(maxCount), "maxCount must be greater than or equal to minCount or be negative if no maximum is specified.");

			Rule = ruleId;
			MinCount = minCount;
			MaxCount = Math.Max(maxCount, -1);
			ParsedValueFactory = parsedValueFactory ?? DefaultParsedValueFactory;
		}

		private static object? DefaultParsedValueFactory(List<ParsedRule> rules) => rules.Select(t => t.parsedValue).ToList();

		public override ParsedRule Parse(ParserContext context)
		{
			var rules = new List<ParsedRule>();
			var initialPosition = context.position;

			for (int i = 0; i < this.MaxCount || this.MaxCount == -1; i++)
			{
				ParsedRule parsedRule = ParsedRule.Fail;
				if (!context.parser.TryParseRule(Rule, context, out parsedRule))
				{
					break;
				}
				context.position = parsedRule.startIndex + parsedRule.length;
				rules.Add(parsedRule.WithOccurency(i));
			}

			if (rules.Count < MinCount)
			{
				throw new ParsingException($"Expected at least {MinCount} repetitions of {GetRule(Rule)}\nbut found {rules.Count}.",
					context.str, initialPosition);
			}

			return new ParsedRule(
				Id,
				initialPosition,
				context.position - initialPosition,
				rules.ToImmutableList(),
				ParsedValueFactory(rules));

		}

		public override bool TryParse(ParserContext context, out ParsedRule result)
		{
			var rules = new List<ParsedRule>();
			var initialPosition = context.position;

			for (int i = 0; i < this.MaxCount || this.MaxCount == -1; i++)
			{
				ParsedRule parsedRule = ParsedRule.Fail;
				if (!context.parser.TryParseRule(Rule, context, out parsedRule))
				{
					break;
				}
				context.position = parsedRule.startIndex + parsedRule.length;
				rules.Add(parsedRule.WithOccurency(i));
			}

			if (rules.Count < MinCount)
			{
				context.errors.Add(new ParsingError(context.position,
					$"Expected at least {MinCount} repetitions of {GetRule(Rule)}\nbut found {rules.Count}."));
				result = ParsedRule.Fail;
				return false;
			}

			result = new ParsedRule(
				Id,
				initialPosition,
				context.position - initialPosition,
				rules.ToImmutableList(),
				ParsedValueFactory(rules));

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
			return obj is RepeatParserRule rule &&
				   Rule == rule.Rule &&
				   MinCount == rule.MinCount &&
				   MaxCount == rule.MaxCount &&
				   ParsedValueFactory == rule.ParsedValueFactory;
		}

		public override int GetHashCode()
		{
			int hashCode = -1997225606;
			hashCode = hashCode * -1521134295 + Rule.GetHashCode();
			hashCode = hashCode * -1521134295 + MinCount.GetHashCode();
			hashCode = hashCode * -1521134295 + MaxCount.GetHashCode();
			hashCode = hashCode * -1521134295 + ParsedValueFactory.GetHashCode();
			return hashCode;
		}
	}
}