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

		protected override TokenPattern BuildToken(List<int>? tokenChildren)
		{
			return new SequenceTokenPattern(tokenChildren);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableSequenceTokenPattern other &&
				   Elements.SequenceEqual(other.Elements);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode ^= Elements.GetSequenceHashCode() * 23;
			return hashCode;
		}
	}
}
