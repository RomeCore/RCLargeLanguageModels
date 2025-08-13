using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
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
		/// Initializes a new instance of the <see cref="SequenceParserRule"/> class.
		/// </summary>
		/// <param name="parserRules">The rules ids that make up the sequence.</param>
		public SequenceParserRule(IEnumerable<int> parserRules)
		{
			Rules = parserRules?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(parserRules));
			if (Rules.Length == 0)
				throw new ArgumentException("Sequence must have at least one rule");
		}



		public override bool TryParse(ParserContext context, ParserContext childContext, out ParsedRule result)
		{
			var startIndex = childContext.position;
			var rules = new List<ParsedRule>();
			int i = 0;

			foreach (var rule in Rules)
			{
				if (!TryParseRule(rule, childContext, out var parsedRule))
				{
					RecordError(childContext, $"Failed to parse sequence rule.");
					result = ParsedRule.Fail;
					return false;
				}

				parsedRule.occurency = i++;
				rules.Add(parsedRule);
				childContext.position = parsedRule.startIndex + parsedRule.length;
			}

			result = new ParsedRule(Id, startIndex, childContext.position - startIndex, rules);

			return true;
		}



		public override string ToString(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return "Sequence...";
			return $"Sequence:\n" +
				string.Join("\n", Rules.Select(c => GetRule(c).ToString(remainingDepth - 1)))
				.Indent("  ");
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