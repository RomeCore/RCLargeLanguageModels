using System;
using System.Collections.Generic;

namespace RCLargeLanguageModels.Parsing.Building.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that is a leaf node. This means it does not have any children.
	/// </summary>
	public sealed class BuildableLeafTokenPattern : BuildableTokenPattern
	{
		/// <summary>
		/// Gets or sets the token pattern to build.
		/// </summary>
		public TokenPattern TokenPattern { get; set; }

		public override IEnumerable<Or<string, BuildableTokenPattern>>? Children => null;

		public override TokenPattern Build(List<int>? children)
		{
			return TokenPattern ?? throw new ParserBuildingException("Token pattern cannot be null.");
		}

		public override bool Equals(object? obj)
		{
			return obj is BuildableLeafTokenPattern pattern &&
				   Equals(TokenPattern, pattern.TokenPattern);
		}

		public override int GetHashCode()
		{
			int hashCode = 17;
			hashCode ^= (TokenPattern?.GetHashCode() ?? 0) * 23;
			return hashCode;
		}
	}
}