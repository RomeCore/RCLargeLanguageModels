using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace RCLargeLanguageModels.Parsing
{
	/// <summary>
	/// Represents a parsed rule in the grammar.
	/// </summary>
	public readonly struct ParsedRule
	{
		/// <summary>
		/// Gets the value of the rule if it was successful.
		/// </summary>
		public readonly bool success;

		/// <summary>
		/// The ID of the rule that was parsed.
		/// </summary>
		public readonly int ruleId;

		/// <summary>
		/// The starting index of the rule in the input text.
		/// </summary>
		public readonly int startIndex;

		/// <summary>
		/// The length of the rule in the input text.
		/// </summary>
		public readonly int length;

		/// <summary>
		/// Gets the value indicating whether this rule represents a token.
		/// </summary>
		public readonly bool isToken;

		/// <summary>
		/// The single token associated with this rule.
		/// </summary>
		public readonly ParsedToken token;

		/// <summary>
		/// Gets the children rules of this rule.
		/// </summary>
		public readonly ImmutableArray<ParsedRule> rules;

		/// <summary>
		/// Gets the parsed value associated with this rule.
		/// </summary>
		public readonly object? parsedValue;

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsedRule"/> struct.
		/// </summary>
		/// <param name="ruleId">The ID of the rule that was parsed.</param>
		/// <param name="startIndex">The starting index of the rule in the input text.</param>
		/// <param name="length">The length of the rule in the input text.</param>
		/// <param name="token">The single token associated with this rule.</param>
		/// <param name="parsedValue">The parsed value associated with this rule.</param>
		public ParsedRule(int ruleId, int startIndex, int length, ParsedToken token, object? parsedValue = null)
		{
			this.success = true;
			this.ruleId = ruleId;
			this.startIndex = startIndex;
			this.length = length;
			this.isToken = true;
			this.token = token;
			this.rules = ImmutableArray<ParsedRule>.Empty;
			this.parsedValue = parsedValue;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsedRule"/> struct.
		/// </summary>
		/// <param name="success">The value of the rule if it was successful.</param>
		/// <param name="ruleId">The ID of the rule that was parsed.</param>
		/// <param name="startIndex">The starting index of the rule in the input text.</param>
		/// <param name="length">The length of the rule in the input text.</param>
		/// <param name="rules">The children rules of this rule.</param>
		/// <param name="parsedValue">The parsed value associated with this rule.</param>
		public ParsedRule(bool success, int ruleId, int startIndex, int length, ImmutableArray<ParsedRule> rules, object? parsedValue = null)
		{
			this.success = success;
			this.ruleId = ruleId;
			this.startIndex = startIndex;
			this.length = length;
			this.isToken = false;
			this.token = ParsedToken.Fail;
			this.rules = rules;
			this.parsedValue = parsedValue;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsedRule"/> struct.
		/// </summary>
		/// <param name="success">The value of the rule if it was successful.</param>
		/// <param name="ruleId">The ID of the rule that was parsed.</param>
		/// <param name="startIndex">The starting index of the rule in the input text.</param>
		/// <param name="length">The length of the rule in the input text.</param>
		/// <param name="isToken">The value indicating whether this rule represents a token.</param>
		/// <param name="token">The single token associated with this rule.</param>
		/// <param name="rules">The children rules of this rule.</param>
		/// <param name="parsedValue">The parsed value associated with this rule.</param>
		private ParsedRule(bool success, int ruleId, int startIndex, int length, bool isToken, ParsedToken token, ImmutableArray<ParsedRule> rules, object? parsedValue = null)
		{
			this.success = success;
			this.ruleId = ruleId;
			this.startIndex = startIndex;
			this.length = length;
			this.isToken = isToken;
			this.token = token;
			this.rules = rules;
			this.parsedValue = parsedValue;
		}

		/// <summary>
		/// Gets a parsed rule that represents failure.
		/// </summary>
		public static ParsedRule Fail { get; } = new ParsedRule(false, -1, -1, 0, false, ParsedToken.Fail, ImmutableArray<ParsedRule>.Empty, null);

		/// <summary>
		/// Gets a text contents of the parsed token.
		/// </summary>
		/// <param name="input">The input text.</param>
		/// <returns>The text representation of the parsed token.</returns>
		public string GetText(string input)
		{
			if (string.IsNullOrEmpty(input))
				throw new ArgumentException("Input text cannot be null or empty.", nameof(input));
			if (!success)
				throw new InvalidOperationException("Cannot get text for a failed token.");

			return input.Substring(startIndex, length);
		}

		/// <summary>
		/// Gets a text contents of the parsed token.
		/// </summary>
		/// <param name="context">The parser context that contains the input text.</param>
		/// <returns>The text representation of the parsed token.</returns>
		public string GetText(ParserContext context)
		{
			if (string.IsNullOrEmpty(context.str))
				throw new ArgumentException("Input text cannot be null or empty.", nameof(context));
			if (!success)
				throw new InvalidOperationException("Cannot get text for a failed token.");

			return context.str.Substring(startIndex, length);
		}

		/// <summary>
		/// Gets the value of the parsed token.
		/// </summary>
		/// <returns>The value of the parsed token.</returns>
		/// <remarks>
		/// Throws an exception if this result has no value or is a failure.
		/// </remarks>
		public object GetValue()
		{
			if (!success)
				throw new InvalidOperationException("Cannot get value for a failed rule.");
			if (parsedValue == null)
				throw new InvalidOperationException("Parsed value is not set.");
			return parsedValue;
		}

		/// <summary>
		/// Gets the value of the parsed token of the specified type.
		/// </summary>
		/// <returns>The value of the parsed token.</returns>
		/// <remarks>
		/// Throws an exception if this result has no value of type <typeparamref name="T"/> or is a failure.
		/// </remarks>
		public T GetValue<T>()
		{
			if (!success)
				throw new InvalidOperationException("Cannot get value for a failed rule.");
			if (parsedValue == null)
				throw new InvalidOperationException("Parsed value is not set.");
			if (parsedValue is T result)
				return result;
			throw new InvalidCastException($"The parsed value cannot be cast to {typeof(T)}, it is of type {parsedValue.GetType()}.");
		}
	}
}