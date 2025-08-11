using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

namespace RCLargeLanguageModels.Parsing.ParserRules
{
	/// <summary>
	/// Represents a parser rule that is optional.
	/// </summary>
	public class OptionalParserRule : ParserRule
	{
		/// <summary>
		/// Gets the rule ID that is optional.
		/// </summary>
		public int Rule { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OptionalParserRule"/> class.
		/// </summary>
		/// <param name="rule">The rule ID that is optional.</param>
		public OptionalParserRule(int rule)
		{
			Rule = rule;
		}



		public override bool TryParse(ParserContext context, ParserContext childContext, out ParsedRule result)
		{
			if (context.parser.TryParseRule(Rule, childContext, out var parsedRule))
			{
				result = new ParsedRule(Id, parsedRule.startIndex, parsedRule.length, ImmutableList.Create(parsedRule), ParsedValueFactory, parsedRule.intermediateValue);
				return true;
			}
			else
			{
				result = new ParsedRule(Id, context.position, 0, ImmutableList<ParsedRule>.Empty, ParsedValueFactory, null);
				return true;
			}
		}



		public override string ToString(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return "Optional...";
			return $"Optional: {GetRule(Rule).ToString(remainingDepth - 1)}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is OptionalParserRule rule &&
				   Rule == rule.Rule;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + Rule.GetHashCode();
			return hashCode;
		}
	}
}