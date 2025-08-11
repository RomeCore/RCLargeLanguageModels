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
			AdvanceContext(ref context);

			if (context.position >= context.str.Length)
			{
				token = new ParsedToken(Id, context.str.Length, 0, ParsedValueFactory, null);
				return true;
			}

			token = ParsedToken.Fail;
			return false;
		}



		public override bool Equals(object obj)
		{
			return base.Equals(obj) &&
				   obj is EOFTokenPattern;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override string ToString(int remainingDepth)
		{
			return "[EOF]";
		}
	}
}