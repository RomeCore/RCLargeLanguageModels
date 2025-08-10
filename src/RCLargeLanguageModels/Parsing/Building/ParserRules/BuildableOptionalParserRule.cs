using System;
using System.Collections.Generic;
using RCLargeLanguageModels.Parsing.ParserRules;

namespace RCLargeLanguageModels.Parsing.Building.ParserRules
{
	/// <summary>
	/// Represents a buildable optional parser rule.
	/// </summary>
	public class BuildableOptionalParserRule : BuildableParserRule
	{
		/// <summary>
		/// The child of this parser rule.
		/// </summary>
		public Or<string, BuildableParserRule> Child { get; set; } = string.Empty;
		public override IEnumerable<Or<string, BuildableParserRule>>? RuleChildren => Child.WrapIntoEnumerable();
		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren => null;

		/// <summary>
		/// The factory function that creates a parsed value from the matched rule.
		/// </summary>
		public Func<ParsedRule?, object?>? ParsedValueFactory { get; set; } = null;

		protected override ParserRule BuildRule(List<int>? ruleChildren, List<int>? tokenChildren)
		{
			return new OptionalParserRule(ruleChildren[0], ParsedValueFactory);
		}

		public override bool Equals(object? obj)
		{
			return obj is BuildableOptionalParserRule other &&
				   Child == other.Child &&
				   Equals(ParsedValueFactory, other.ParsedValueFactory);
		}

		public override int GetHashCode()
		{
			int hashCode = 17;
			hashCode ^= Child.GetHashCode() * 23;
			hashCode ^= (ParsedValueFactory?.GetHashCode() ?? 0) * 47;
			return hashCode;
		}
	}
}