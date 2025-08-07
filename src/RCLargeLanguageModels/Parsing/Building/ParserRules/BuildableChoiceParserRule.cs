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
		public override IEnumerable<Or<string, BuildableParserRule>>? Children => Choices;

		/// <summary>
		/// The factory function that creates a parsed value from the matched rule.
		/// </summary>
		public Func<ParsedRule, object?>? ParsedValueFactory { get; set; } = null;

		public override ParserRule Build(List<int>? children)
		{
			return new ChoiceParserRule(children, ParsedValueFactory);
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