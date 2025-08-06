using System;
using System.Collections.Generic;
using System.Text;
using RCLargeLanguageModels.Parsing.TokenPatterns;

namespace RCLargeLanguageModels.Parsing.Building
{
	/// <summary>
	/// Represents a builder for constructing tokens for parsing.
	/// </summary>
	public class TokenBuilder
	{
		/// <summary>
		/// Gets or sets the token being built.
		/// </summary>
		public BuildableTokenPattern? Pattern { get; set; }


	}
}