using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Parsing.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that matches the end of file.
	/// </summary>
	public class EOFTokenPattern : TokenPattern
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EOFTokenPattern"/> class.
		/// </summary>
		public EOFTokenPattern()
		{
		}

		public override bool TryMatch(ParserContext context, out ParsedToken token)
		{
			if (context.position >= context.str.Length)
			{
				token = new ParsedToken(Id, context.position, 0);
				return true;
			}
			token = ParsedToken.Fail;
			return false;
		}

		public override bool Equals(object obj)
		{
			return obj is EOFTokenPattern;
		}

		public override int GetHashCode()
		{
			return 0;
		}

		public override string ToString(int remainingDepth)
		{
			return "[EOF]";
		}
	}
}