using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RCLargeLanguageModels.Parsing.TokenPatterns;

namespace RCLargeLanguageModels.Parsing.Building.TokenPatterns
{
	/// <summary>
	/// Represents a buildable sequence parser rule.
	/// </summary>
	public class BuildableSequenceTokenPattern : BuildableTokenPattern
	{
		/// <summary>
		/// The elements of the sequence parser rule.
		/// </summary>
		public List<Or<string, BuildableTokenPattern>> Elements { get; } = new List<Or<string, BuildableTokenPattern>>();
		public override IEnumerable<Or<string, BuildableTokenPattern>>? Children => Elements;

		/// <summary>
		/// The factory method to create a parsed value from the matched rules.
		/// </summary>
		public Func<List<ParsedToken>, object?>? ParsedValueFactory { get; set; } = null;

		public override TokenPattern Build(List<int>? children)
		{
			return new SequenceTokenPattern(children, ParsedValueFactory);
		}

		public override bool Equals(object? obj)
		{
			return obj is BuildableSequenceTokenPattern other &&
				   Elements.SequenceEqual(other.Elements) &&
				   Equals(ParsedValueFactory, other.ParsedValueFactory);
		}

		public override int GetHashCode()
		{
			int hashCode = 17;
			hashCode ^= Elements.GetSequenceHashCode() * 23;
			hashCode ^= ParsedValueFactory.GetHashCode() * 47;
			return hashCode;
		}
	}
}
