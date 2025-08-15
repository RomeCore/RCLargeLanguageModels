using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Parsing.TokenPatterns
{
	/// <summary>
	/// The token pattern that matches one or more whitespace characters.
	/// </summary>
	public class WhitespacesTokenPattern : RepeatCharactersTokenPattern
	{
		/// <summary>
		/// Creates a new instance of the <see cref="WhitespacesTokenPattern"/> class.
		/// </summary>
		public WhitespacesTokenPattern() : base(char.IsWhiteSpace, 1, -1)
		{
		}

		public override string ToStringOverride(int remainingDepth)
		{
			return "[WS]";
		}
	}
}