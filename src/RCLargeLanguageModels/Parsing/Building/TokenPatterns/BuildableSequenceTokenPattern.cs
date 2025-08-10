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
		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren => Elements;

		/// <summary>
		/// The factory method to create a parsed value from the matched rules.
		/// </summary>
		public Func<List<ParsedToken>, object?>? ParsedValueFactory { get; set; } = null;

		protected override TokenPattern BuildToken(List<int>? tokenChildren)
		{
			return new SequenceTokenPattern(tokenChildren, ParsedValueFactory);
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
			hashCode ^= (ParsedValueFactory?.GetHashCode() ?? 0) * 47;
			return hashCode;
		}
	}
}
