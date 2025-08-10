using System;
using System.Collections.Generic;
using System.Text;
using RCLargeLanguageModels.Parsing.TokenPatterns;

namespace RCLargeLanguageModels.Parsing.Building.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that can be built into a repeat pattern.
	/// </summary>
	public class BuildableRepeatTokenPattern : BuildableTokenPattern
	{
		/// <summary>
		/// Gets or sets the child of this token pattern.
		/// </summary>
		public Or<string, BuildableTokenPattern> Child { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the minimum number of times the child pattern can be repeated.
		/// </summary>
		public int MinCount { get; set; } = 0;

		/// <summary>
		/// Gets or sets the maximum number of times the child pattern can be repeated. -1 indicates no upper limit.
		/// </summary>
		public int MaxCount { get; set; } = -1;

		/// <summary>
		/// Gets the children of this token pattern.
		/// </summary>
		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren => Child.WrapIntoEnumerable();

		/// <summary>
		/// Gets or sets the factory that creates a parsed value from the matched token.
		/// </summary>
		public Func<List<ParsedToken>, object?>? ParsedValueFactory { get; set; } = null;

		protected override TokenPattern BuildToken(List<int>? tokenChildren)
		{
			return new RepeatTokenPattern(tokenChildren[0], MinCount, MaxCount, ParsedValueFactory);
		}

		public override bool Equals(object? obj)
		{
			return obj is BuildableRepeatTokenPattern other &&
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
			hashCode ^= (ParsedValueFactory?.GetHashCode() ?? 0) * 47;
			return hashCode;
		}
	}
}