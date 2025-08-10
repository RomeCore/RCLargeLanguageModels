using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RCLargeLanguageModels.Parsing.ParserRules;

namespace RCLargeLanguageModels.Parsing.Building.ParserRules
{
	/// <summary>
	/// Represents a buildable sequence parser rule.
	/// </summary>
	public class BuildableSequenceParserRule : BuildableParserRule
	{
		/// <summary>
		/// The elements of the sequence parser rule.
		/// </summary>
		public List<Or<string, BuildableParserRule>> Elements { get; } = new List<Or<string, BuildableParserRule>>();
		public override IEnumerable<Or<string, BuildableParserRule>>? RuleChildren => Elements;
		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren => null;

		/// <summary>
		/// The factory method to create a parsed value from the matched rules.
		/// </summary>
		public Func<List<ParsedRule>, object?>? ParsedValueFactory { get; set; } = null;

		protected override ParserRule BuildRule(List<int>? ruleChildren, List<int>? tokenChildren)
		{
			return new SequenceParserRule(ruleChildren, ParsedValueFactory);
		}

		public override bool Equals(object? obj)
		{
			return obj is BuildableSequenceParserRule other &&
				   Elements.SequenceEqual(other.Elements) &&
				   Equals(ParsedValueFactory, other.ParsedValueFactory);
		}

		public override int GetHashCode()
		{
			int hashCode = 17;
			hashCode ^= Elements.GetSequenceHashCode() * 23;
			hashCode ^= (ParsedValueFactory?.GetHashCode() ?? 0) * 47;
			return hashCode;
		}
	}
}