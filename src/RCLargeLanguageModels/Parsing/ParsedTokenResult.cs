using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Text;

namespace RCLargeLanguageModels.Parsing
{
	/// <summary>
	/// Represents the result of a parsed token.
	/// </summary>
	public class ParsedTokenResult
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
		/// Gets the parsed token object containing the result of the parse.
		/// </summary>
		public ParsedToken Result { get; }

		/// <summary>
		/// Gets value indicating whether the parsing operation was successful.
		/// </summary>
		public bool Success => Result.success;

		/// <summary>
		/// Gets the unique identifier for the token that was parsed.
		/// </summary>
		public int TokenId => Result.tokenId;

		/// <summary>
		/// Gets the parsed value associated with this token.
		/// </summary>
		public TokenPattern Token => Context.parser.TokenPatterns[Result.tokenId];

		/// <summary>
		/// Gets the aliases for the token pattern that was parsed.
		/// </summary>
		public ImmutableList<string> TokenAliases => Token.Aliases;

		/// <summary>
		/// Gets the starting index of the token in the input text.
		/// </summary>
		public int StartIndex => Result.startIndex;

		/// <summary>
		/// Gets the length of the token in the input text.
		/// </summary>
		public int Length => Result.length;

		/// <summary>
		/// Gets the intermediate value associated with this token.
		/// </summary>
		public object? IntermediateValue => Result.intermediateValue;

		private readonly Lazy<string> _textLazy;
		/// <summary>
		/// Gets the parsed input text that was parsed.
		/// </summary>
		public string Text => _textLazy.Value;

		private readonly Lazy<object?> _valueLazy;
		/// <summary>
		/// Gets the parsed value associated with this token.
		/// </summary>
		public object? Value => _valueLazy.Value;

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsedTokenResult"/> class.
		/// </summary>
		/// <param name="parent">The parent result of this rule, if any.</param>
		/// <param name="context">The parser context used for parsing.</param>
		/// <param name="result">The parsed token object containing the result of the parse.</param>
		public ParsedTokenResult(ParsedRuleResult? parent, ParserContext context, ParsedToken result)
		{
			Parent = parent;
			Context = context;
			Result = result;

			_textLazy = new Lazy<string>(() => context.str.Substring(result.startIndex, result.length));
			_valueLazy = new Lazy<object?>(() => result.parsedValueFactory?.Invoke(this) ?? null);
		}
	}
}