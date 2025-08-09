using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace RCLargeLanguageModels.Parsing
{
	/// <summary>
	/// Represents the result of a parsed rule.
	/// </summary>
	public class ParsedRuleResult
	{
		/// <summary>
		/// Gets the parent result of this rule, if any.
		/// </summary>
		public ParsedRuleResult? Parent { get; }

		/// <summary>
		/// Gets the parser context used for parsing.
		/// </summary>
		public ParserContext Context { get; }

		/// <summary>
		/// Gets the parsed rule object containing the result of the parse.
		/// </summary>
		public ParsedRule Result { get; }

		/// <summary>
		/// Gets the token result if the parsed result represents a token. Otherwise, returns null.
		/// </summary>
		public ParsedTokenResult? Token { get; }

		/// <summary>
		/// Gets value indicating whether the parsing operation was successful.
		/// </summary>
		public bool Success => Result.success;

		/// <summary>
		/// Gets value indicating whether the parsed result represents a token.
		/// </summary>
		public bool IsToken => Result.isToken;

		/// <summary>
		/// Gets the unique identifier for the parser rule that was parsed.
		/// </summary>
		public int RuleId => Result.ruleId;

		/// <summary>
		/// Gets the parsed value associated with this rule.
		/// </summary>
		public ParserRule Rule => Context.parser.Rules[Result.ruleId];

		/// <summary>
		/// Gets the aliases for the parser rule that was parsed.
		/// </summary>
		public ImmutableList<string> RuleAliases => Rule.Aliases;

		/// <summary>
		/// Gets the starting index of the rule in the input text.
		/// </summary>
		public int StartIndex => Result.startIndex;

		/// <summary>
		/// Gets the length of the rule in the input text.
		/// </summary>
		public int Length => Result.length;

		/// <summary>
		/// Gets the parsed input text that was parsed.
		/// </summary>
		public string Text => Result.GetText(Context);

		/// <summary>
		/// Gets the parsed value associated with this rule. If no value is set, returns <see langword="null"/>.
		/// </summary>
		public object? Value => Result.parsedValue;

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsedRuleResult"/> class.
		/// </summary>
		/// <param name="parent">The parent result of this rule, if any.</param>
		/// <param name="context">The parser context used for parsing.</param>
		/// <param name="result">The parsed rule object containing the result of the parse.</param>
		public ParsedRuleResult(ParsedRuleResult? parent, ParserContext context, ParsedRule result)
		{
			Parent = parent;
			Context = context;
			Result = result;
			Token = result.isToken ? new ParsedTokenResult(this, context, result.token) : null;
		}

		/// <summary>
		/// Gets child parsed rules for this rule.
		/// </summary>
		/// <returns>A collection of child parsed rules. Returns an empty collection if no children are present.</returns>
		public IEnumerable<ParsedRuleResult> GetChildren()
		{
			return Result.rules.Select(r => new ParsedRuleResult(this, Context, r));
		}

		/// <summary>
		/// Gets child parsed rules for this rule and joins them into a single collection.
		/// </summary>
		/// <param name="maxDepth">The maximum depth to which child rules should be joined. If less than or equal to zero, this element is returned.</param>
		/// <returns>A collection of child parsed rules. Returns this element if no children are present or the maximum depth is reached.</returns>
		public IEnumerable<ParsedRuleResult> GetJoinedChildren(int maxDepth)
		{
			if (maxDepth <= 0 || Result.rules.Count == 0)
				return this.WrapIntoEnumerable();

			return Result.rules.SelectMany(r => new ParsedRuleResult(this, Context, r).GetJoinedChildren(maxDepth - 1));
		}
	}
}