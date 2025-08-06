using System;
using System.Collections.Generic;
using RCLargeLanguageModels.Parsing.TokenPatterns;

namespace RCLargeLanguageModels.Parsing.Building.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that can be built into an optional pattern.
	/// </summary>
	public class BuildableOptionalTokenPattern : BuildableTokenPattern
	{
		/// <summary>
		/// The child of this token pattern.
		/// </summary>
		public Or<string, BuildableTokenPattern> Child { get; set; } = string.Empty;
		public override IEnumerable<Or<string, BuildableTokenPattern>>? Children => Child.WrapIntoEnumerable();

		/// <summary>
		/// The factory function that creates a parsed value from the matched token.
		/// </summary>
		public Func<ParsedToken?, object?>? ParsedValueFactory { get; set; } = null;

		public override TokenPattern Build(List<int>? children)
		{
			return new OptionalTokenPattern(children[0], ParsedValueFactory);
		}

		public override bool Equals(object? obj)
		{
			return obj is BuildableOptionalTokenPattern other &&
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