using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RCLargeLanguageModels.Parsing.TokenPatterns;

namespace RCLargeLanguageModels.Parsing.Building.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that can be built into a choice of multiple patterns.
	/// </summary>
	public class BuildableChoiceTokenPattern : BuildableTokenPattern
	{
		/// <summary>
		/// The choices of this token pattern.
		/// </summary>
		public List<Or<string, BuildableTokenPattern>> Choices { get; } = new List<Or<string, BuildableTokenPattern>>();
		public override IEnumerable<Or<string, BuildableTokenPattern>>? Children => Choices;

		/// <summary>
		/// The factory function that creates a parsed value from the matched token.
		/// </summary>
		public Func<ParsedToken, object?>? ParsedValueFactory { get; set; } = null;

		public override TokenPattern Build(List<int>? children)
		{
			return new ChoiceTokenPattern(children, ParsedValueFactory);
		}

		public override bool Equals(object? obj)
		{
			return obj is BuildableChoiceTokenPattern other &&
				   Choices.SequenceEqual(other.Choices) &&
				   Equals(ParsedValueFactory, other.ParsedValueFactory);
		}

		public override int GetHashCode()
		{
			int hashCode = 17;
			hashCode ^= Choices.GetSequenceHashCode() * 23;
			hashCode ^= ParsedValueFactory.GetHashCode() * 47;
			return hashCode;
		}
	}
}