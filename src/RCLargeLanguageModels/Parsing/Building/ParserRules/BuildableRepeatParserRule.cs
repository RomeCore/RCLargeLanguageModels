using System;
using System.Collections.Generic;
using System.Text;
using RCLargeLanguageModels.Parsing.ParserRules;

namespace RCLargeLanguageModels.Parsing.Building.ParserRules
{
	public class BuildableRepeatParserRule : BuildableParserRule
	{
		/// <summary>
		/// Gets or sets the child of this parser rule.
		/// </summary>
		public Or<string, BuildableParserRule> Child { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the minimum number of times the child pattern can be repeated.
		/// </summary>
		public int MinCount { get; set; } = 0;

		/// <summary>
		/// Gets or sets the maximum number of times the child pattern can be repeated. -1 indicates no upper limit.
		/// </summary>
		public int MaxCount { get; set; } = -1;

		/// <summary>
		/// Gets the children of this parser rule.
		/// </summary>
		public override IEnumerable<Or<string, BuildableParserRule>>? Children => Child.WrapIntoEnumerable();

		/// <summary>
		/// Gets or sets the factory that creates a parsed value from the matched token.
		/// </summary>
		public Func<List<ParsedRule>, object?>? ParsedValueFactory { get; set; } = null;

		/// <summary>
		/// Builds the parser rule with the given children.
		/// </summary>
		/// <param name="children">The children IDs of this parser rule.</param>
		/// <returns>The built parser rule.</returns>
		public override ParserRule Build(List<int>? children)
		{
			return new RepeatParserRule(children[0], MinCount, MaxCount, ParsedValueFactory);
		}

		public override bool Equals(object? obj)
		{
			return obj is BuildableRepeatParserRule other &&
				   Child == other.Child &&
				   MinCount == other.MinCount &&
				   MaxCount == other.MaxCount &&
				   Equals(ParsedValueFactory, other.ParsedValueFactory);
		}

		public override int GetHashCode()
		{
			int hashCode = 17;
			hashCode ^= Child.GetHashCode() * 23;
			hashCode ^= MinCount.GetHashCode() * 29;
			hashCode ^= MaxCount.GetHashCode() * 31;
			hashCode ^= ParsedValueFactory.GetHashCode() * 47;
			return hashCode;
		}
	}
}