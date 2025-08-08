using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Parsing.TokenPatterns
{
	public class LiteralChoiceTokenPattern : TokenPattern
	{
		public override bool TryMatch(ParserContext context, out ParsedToken token)
		{
			throw new NotImplementedException();
		}

		public override string ToString(int remainingDepth)
		{
			throw new NotImplementedException();
		}
	}
}