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
		/// Initializes a new instance of the <see cref="ParsedRule"/> struct.
		/// </summary>
		/// <param name="ruleId">The ID of the rule that was parsed.</param>
		/// <param name="startIndex">The starting index of the rule in the input text.</param>
		/// <param name="length">The length of the rule in the input text.</param>
		/// <param name="token">The single token associated with this rule.</param>
		public ParsedRule(int ruleId, int startIndex, int length, ParsedToken token)
		{
			this.success = true;
			this.ruleId = ruleId;
			this.startIndex = startIndex;
			this.length = length;
			this.isToken = true;
			this.token = token;
			this.rules = ImmutableArray<ParsedRule>.Empty;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsedRule"/> struct.
		/// </summary>
		/// <param name="success">The value of the rule if it was successful.</param>
		/// <param name="ruleId">The ID of the rule that was parsed.</param>
		/// <param name="startIndex">The starting index of the rule in the input text.</param>
		/// <param name="length">The length of the rule in the input text.</param>
		/// <param name="rules">The children rules of this rule.</param>
		public ParsedRule(bool success, int ruleId, int startIndex, int length, ImmutableArray<ParsedRule> rules)
		{
			this.success = success;
			this.ruleId = ruleId;
			this.startIndex = startIndex;
			this.length = length;
			this.isToken = false;
			this.token = ParsedToken.Fail;
			this.rules = rules;
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
		private ParsedRule(bool success, int ruleId, int startIndex, int length, bool isToken, ParsedToken token, ImmutableArray<ParsedRule> rules)
		{
			this.success = success;
			this.ruleId = ruleId;
			this.startIndex = startIndex;
			this.length = length;
			this.isToken = isToken;
			this.token = token;
			this.rules = rules;
		}

		/// <summary>
		/// Gets a parsed rule that represents failure.
		/// </summary>
		public static ParsedRule Fail { get; } = new ParsedRule(false, -1, -1, 0, false, ParsedToken.Fail, ImmutableArray<ParsedRule>.Empty);
	}
}