using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace RCLargeLanguageModels.Parsing.ParserRules
{
	public class ChoiceParserRule : ParserRule
	{
		/// <summary>
		/// The rule ids that are being chosen from.
		/// </summary>
		public ImmutableArray<int> Choices { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChoiceParserRule"/> class.
		/// </summary>
		/// <param name="parserRuleIds">The parser rules ids to choose from.</param>
		public ChoiceParserRule(IEnumerable<int> parserRuleIds)
		{
			Choices = parserRuleIds?.ToImmutableArray()
				?? throw new ArgumentNullException(nameof(parserRuleIds));
			if (Choices.IsEmpty)
				throw new ArgumentException("At least one parser rule must be provided.", nameof(parserRuleIds));
		}



		public override ParsedRule Parse(ParserContext context, ParserContext childContext)
		{
			List<ParsedRule> fails = new ();

			int i = 0;
			foreach (var rule in Choices)
			{
				var parsedRule = TryParseRule(rule, childContext);
				if (parsedRule.success)
				{
					parsedRule.occurency = i;
					return ParsedRule.Rule(Id,
						parsedRule.startIndex,
						parsedRule.length,
						new List<ParsedRule> { parsedRule },
						parsedRule.intermediateValue);
				}
				fails.Add(parsedRule);
				i++;
			}

			RecordError(context, "Found no matching choice.");
			return ParsedRule.Fail;
		}



		public override string ToStringOverride(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return "Choice...";
			return $"Choice:\n" +
				string.Join("\n", Choices.Select(c => Parser.Rules[c].ToString(remainingDepth - 1)))
				.Indent("  ");
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is ChoiceParserRule rule &&
				   Choices.SequenceEqual(rule.Choices);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + Choices.GetSequenceHashCode();
			return hashCode;
		}
	}
}