using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Parsing.Building
{
	/// <summary>
	/// Represents a buildable parser rule. This is an abstract base class that represents a parser rule that can be built.
	/// </summary>
	/// <remarks>
	/// Its recommended to implement the Equals and GetHashCode methods to remove redudancy when compiling parser.
	/// </remarks>
	public abstract class BuildableParserRule
	{
		/// <summary>
		/// Gets the children of this parser rule. Each child can be name reference or a buildable parser rule.
		/// </summary>
		public abstract IEnumerable<Or<string, BuildableParserRule>>? Children { get; }

		/// <summary>
		/// Gets the token children of this parser rule. Each child can be a name reference or a buildable token pattern.
		/// </summary>
		public abstract IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren { get; }

		/// <summary>
		/// Builds the parser rule with the given children.
		/// </summary>
		/// <param name="children">The children IDs to build the parser rule with.</param>
		/// <param name="tokenChildren">The token children IDs to build the parser rule with.</param>
		/// <returns>A token pattern representing the built parser rule.</returns>
		public abstract ParserRule Build(List<int>? children, List<int>? tokenChildren);
	}
}