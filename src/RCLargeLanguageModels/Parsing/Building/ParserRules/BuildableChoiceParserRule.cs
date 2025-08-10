using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RCLargeLanguageModels.Parsing.ParserRules;

namespace RCLargeLanguageModels.Parsing.Building.ParserRules
{
	/// <summary>
	/// Represents a parser rule that can be built into a choice of multiple rules.
	/// </summary>
	public class BuildableChoiceParserRule : BuildableParserRule
	{
		/// <summary>
		/// The choices of this parser rule.
		/// </summary>
		public List<Or<string, BuildableParserRule>> Choices { get; } = new List<Or<string, BuildableParserRule>>();
		public override IEnumerable<Or<string, BuildableParserRule>>? RuleChildren => Choices;
		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren => null;

		/// <summary>
		/// The factory function that creates a parsed value from the matched rule.
		/// </summary>
		public Func<ParsedRule, object?>? ParsedValueFactory { get; set; } = null;

		protected override ParserRule BuildRule(List<int>? ruleChildren, List<int>? tokenChildren)
		{
			return new ChoiceParserRule(ruleChildren, ParsedValueFactory);
		}

		public override bool Equals(object? obj)
		{
			return obj is BuildableChoiceParserRule other &&
				   Choices.SequenceEqual(other.Choices) &&
				   Equals(ParsedValueFactory, other.ParsedValueFactory);
		}

		public override int GetHashCode()
		{
			int hashCode = 17;
			hashCode ^= Choices.GetSequenceHashCode() * 23;
			hashCode ^= (ParsedValueFactory?.GetHashCode() ?? 0) * 47;
			return hashCode;
		}
	}
}