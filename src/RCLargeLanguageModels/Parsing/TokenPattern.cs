using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Parsing
{
	/// <summary>
	/// Represents a pattern that can be used to match tokens in a text.
	/// </summary>
	public abstract class TokenPattern : ParserElement
	{
		/// <summary>
		/// Gets the parsed value factory associated with this token.
		/// </summary>
		public Func<ParsedTokenResult, object?>? ParsedValueFactory { get; internal set; } = null;

		/// <summary>
		/// Tries to match the given context with this pattern.
		/// </summary>
		/// <param name="context">The current parsing context to use for this element.</param>
		/// <param name="childContext">The parsing context to use for child elements.</param>
		/// <param name="token">The parsed token if the match is successful. Otherwise <see langword="null"/>.</param>
		/// <returns><see langword="true"/> if the pattern matches; otherwise, <see langword="false"/>.</returns>
		public abstract bool TryMatch(ParserContext context, ParserContext childContext, out ParsedToken token);

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is TokenPattern other &&
				   Equals(ParsedValueFactory, other.ParsedValueFactory);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode ^= (ParsedValueFactory?.GetHashCode() ?? 0) * 23;
			return hashCode;
		}
	}
}