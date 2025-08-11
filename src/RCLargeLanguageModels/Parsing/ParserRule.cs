using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RCLargeLanguageModels.Parsing
{
	/// <summary>
	/// Represents a parser rule that can be used to parse input strings.
	/// </summary>
	public abstract class ParserRule : ParserElement
	{
		/// <summary>
		/// Gets the parsed value factory associated with this rule.
		/// </summary>
		public Func<ParsedRuleResult, object?>? ParsedValueFactory { get; internal set; } = null;

		/// <summary>
		/// Tries to parse the input string using this rule.
		/// </summary>
		/// <param name="context">The local parser context to use for this element.</param>
		/// <param name="childContext">The parser context for the child elements.</param>
		/// <param name="result">The parsed rule if parsing is successful; otherwise, <see langword="null"/>.</param>
		/// <returns><see langword="true"/> if the input string can be successfully parsed using this rule; otherwise, <see langword="false"/>.</returns>
		public abstract bool TryParse(ParserContext context, ParserContext childContext, out ParsedRule result);

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is ParserRule other &&
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