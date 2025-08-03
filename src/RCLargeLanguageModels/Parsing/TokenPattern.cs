using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Parsing
{
	/// <summary>
	/// Represents a pattern that can be used to match tokens in a text.
	/// </summary>
	public abstract class TokenPattern
	{
		/// <summary>
		/// Tries to match the given context with this pattern.
		/// </summary>
		/// <param name="thisTokenId">The ID of the current token being parsed.</param>
		/// <param name="context">The current parsing context.</param>
		/// <param name="token">The parsed token if the match is successful. Otherwise <see langword="null"/>.</param>
		/// <returns><see langword="true"/> if the pattern matches; otherwise, <see langword="false"/>.</returns>
		public abstract bool TryMatch(int thisTokenId, ParserContext context, out ParsedToken token);
	}
}