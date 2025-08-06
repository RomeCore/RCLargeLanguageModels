using System;
using System.Collections.Generic;
using System.Xml.Linq;
using RCLargeLanguageModels.Parsing.ParserRules;

namespace RCLargeLanguageModels.Parsing.Building.ParserRules
{
	/// <summary>
	/// Represents the buildable entry from parser rule to token pattern.
	/// </summary>
	public sealed class BuildableTokenParserRule : BuildableParserRule
	{
		/// <summary>
		/// Gets or sets the child of this token parser rule. This can be a name reference or a buildable token pattern.
		/// </summary>
		public Or<string, BuildableTokenPattern> Child { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the factory function to create the parsed value from the token.
		/// </summary>
		public Func<ParsedToken, object?>? ParsedValueFactory { get; set; } = null;

		public override IEnumerable<Or<string, BuildableParserRule>>? Children => null;

		public override ParserRule Build(List<int>? children)
		{
			return new TokenParserRule(children[0], ParsedValueFactory);
		}

		public override bool Equals(object? obj)
		{
			return obj is BuildableTokenParserRule other &&
				   Child == other.Child &&
				   Equals(ParsedValueFactory, other.ParsedValueFactory);
		}

		public override int GetHashCode()
		{
			int hashCode = 17;
			hashCode ^= Child.GetHashCode() * 23;
			hashCode ^= ParsedValueFactory.GetHashCode() * 47;
			return hashCode;
		}
	}
}