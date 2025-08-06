using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace RCLargeLanguageModels.Parsing.ParserRules
{
	/// <summary>
	/// Represents a sequence of rules that must be parsed in order.
	/// </summary>
	public class SequenceParserRule : ParserRule
	{
		/// <summary>
		/// The rules ids that make up the sequence.
		/// </summary>
		public ImmutableArray<int> Rules { get; }

		/// <summary>
		/// The factory method to create a parsed value from the matched rules.
		/// </summary>
		public Func<List<ParsedRule>, object?> ParsedValueFactory { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SequenceParserRule"/> class.
		/// </summary>
		/// <param name="parserRules">The rules ids that make up the sequence.</param>
		/// <param name="parsedValueFactory">The factory method to create a parsed value from the matched rules.</param>
		public SequenceParserRule(IEnumerable<int> parserRules, Func<List<ParsedRule>, object?> parsedValueFactory)
		{
			Rules = parserRules?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(parserRules));
			if (Rules.Length == 0)
				throw new ArgumentException("Sequence must have at least one rule");

			ParsedValueFactory = parsedValueFactory ?? DefaultParsedValueFactory;
		}

		private static object? DefaultParsedValueFactory(List<ParsedRule> rules) => rules.Select(t => t.parsedValue).ToList();

		public override bool TryParse(int thisRuleId, ParserContext context, out ParsedRule result)
		{
			var startIndex = context.position;
			var rules = new List<ParsedRule>();
			int i = 0;

			foreach (var rule in Rules)
			{
				if (!context.parser.TryParseRule(rule, context, out var parsedRule))
				{
					context.errors.Add(new ParsingError(context.position, $"Failed to parse rule {context.parser.Rules[rule]}"));
					result = ParsedRule.Fail;
					return false;
				}
				rules.Add(parsedRule.WithOccurency(i++));
				context = context.With(parsedRule.startIndex + parsedRule.length);
			}

			result = new ParsedRule(thisRuleId, startIndex, context.position - startIndex, rules.ToImmutableList(), ParsedValueFactory(rules));
			return true;
		}

		public override ParsedRule Parse(int thisRuleId, ParserContext context)
		{
			var startIndex = context.position;
			var rules = new List<ParsedRule>();
			int i = 0;

			foreach (var rule in Rules)
			{
				if (!context.parser.TryParseRule(rule, context, out var parsedRule))
				{
					throw new ParsingException($"Failed to parse rule {context.parser.Rules[rule]}", context.str, context.position);
				}
				rules.Add(parsedRule.WithOccurency(i++));
				context.position = parsedRule.startIndex + parsedRule.length;
			}

			return new ParsedRule(thisRuleId, startIndex, context.position - startIndex, rules.ToImmutableList(), ParsedValueFactory(rules));
		}

		public override bool Equals(object? obj)
		{
			return obj is SequenceParserRule rule &&
				   Rules.SequenceEqual(rule.Rules) &&
				   ParsedValueFactory == rule.ParsedValueFactory;
		}

		public override int GetHashCode()
		{
			int hashCode = 1930700721;
			hashCode = hashCode * -1521134295 + Rules.GetSequenceHashCode();
			hashCode = hashCode * -1521134295 + ParsedValueFactory.GetHashCode();
			return hashCode;
		}
	}
}