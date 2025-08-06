using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Parsing.Building
{
	/// <summary>
	/// The buildable token pattern. This is an abstract class that represents a token pattern that can be built into a token.
	/// </summary>
	public abstract class BuildableTokenPattern
	{
		/// <summary>
		/// Gets the children of this token pattern. Each child can be name reference or a buildable token pattern.
		/// </summary>
		public abstract IEnumerable<Or<string, BuildableTokenPattern>>? Children { get; }

		/// <summary>
		/// Builds the token pattern with the given children.
		/// </summary>
		/// <param name="children">The children IDs of this token pattern.</param>
		/// <returns>A token pattern representing the built token.</returns>
		public abstract TokenPattern Build(List<int>? children);
	}
}