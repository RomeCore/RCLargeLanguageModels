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
		/// Gets the factory function that creates a parsed value from the matched rule.
		/// </summary>
		public Func<ParsedRule, object?> ParsedValueFactory { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChoiceParserRule"/> class.
		/// </summary>
		/// <param name="parserRuleIds">The parser rules ids to choose from.</param>
		/// <param name="parsedValueFactory">The factory function that creates a parsed value from the selected rule.</param>
		public ChoiceParserRule(IEnumerable<int> parserRuleIds, Func<ParsedRule, object?> parsedValueFactory = null)
		{
			Choices = parserRuleIds?.ToImmutableArray()
				?? throw new ArgumentNullException(nameof(parserRuleIds));
			if (Choices.IsEmpty)
				throw new ArgumentException("At least one parser rule must be provided.", nameof(parserRuleIds));
			ParsedValueFactory = parsedValueFactory ?? DefaultParsedValueFactory;
		}

		private static object? DefaultParsedValueFactory(ParsedRule r) => r.parsedValue;

		public override bool TryParse(int thisRuleId, ParserContext context, out ParsedRule result)
		{
			int i = 0;
			foreach (var rule in Choices)
			{
				if (context.parser.TryParseRule(rule, context, out result))
				{
					result = result.WithRuleId(thisRuleId).WithOccurency(i).WithParsedValue(ParsedValueFactory.Invoke(result));
					return true;
				}
				i++;
			}

			context.errors.Add(new ParsingError(context.position, $"No matching choice found from {ToString(context)}."));
			result = ParsedRule.Fail;
			return false;
		}

		public override ParsedRule Parse(int thisRuleId, ParserContext context)
		{
			List<ParsingException> exceptions = new List<ParsingException>();

			foreach (var rule in Choices)
			{
				try
				{
					return context.parser.ParseRule(rule, context);
				}
				catch (ParsingException ex)
				{
					exceptions.Add(ex);
				}
			}

			throw exceptions[0];
		}

		public override string ToString(ParserContext context)
		{
			return $"Choice:\n" +
				string.Join("\n", Choices.Select(c => context.parser.Rules[c].ToString(context)))
				.Indent("  ");
		}

		public override bool Equals(object? obj)
		{
			return obj is ChoiceParserRule rule &&
				   Choices.SequenceEqual(rule.Choices) &&
				   ParsedValueFactory == rule.ParsedValueFactory;
		}

		public override int GetHashCode()
		{
			int hashCode = 1613406236;
			hashCode = hashCode * -1521134295 + Choices.GetSequenceHashCode();
			hashCode = hashCode * -1521134295 + ParsedValueFactory.GetHashCode();
			return hashCode;
		}
	}
}