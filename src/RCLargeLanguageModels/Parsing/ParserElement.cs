using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace RCLargeLanguageModels.Parsing
{
	/// <summary>
	/// Represents a parser element. This is an abstract base class for both token patterns and rules.
	/// </summary>
	public abstract class ParserElement
	{
		/// <summary>
		/// Gets the unique identifier for this parser element.
		/// </summary>
		public int Id { get; internal set; } = -1;

		/// <summary>
		/// Gets the aliases for this parser element.
		/// </summary>
		public ImmutableList<string> Aliases { get; internal set; } = ImmutableList<string>.Empty;

		/// <summary>
		/// Gets the parser that contains this parser element.
		/// </summary>
		public Parser Parser { get; internal set; } = null!;

		/// <summary>
		/// Gets the local settings for this parser element with each setting configurable by override modes.
		/// </summary>
		public ParserLocalSettings Settings { get; internal set; } = default;

		/// <summary>
		/// Returns a string representation of the parser element using a specified depth for expansion.
		/// </summary>
		/// <param name="remainingDepth">The maximum depth to which the element should be expanded in the string representation. Defaults to 2.</param>
		/// <returns>A string representation of the rule.</returns>
		public abstract string ToString(int remainingDepth);

		/// <summary>
		/// Gets the rule by index within the current parser.
		/// </summary>
		/// <param name="index">The index of the rule to retrieve.</param>
		/// <returns>The rule at the specified index.</returns>
		protected ParserRule GetRule(int index)
		{
			return Parser.Rules[index];
		}

		/// <summary>
		/// Gets the token pattern by index within the current parser.
		/// </summary>
		/// <param name="index">The index of the token pattern to retrieve.</param>
		/// <returns>The token pattern at the specified index.</returns>
		protected TokenPattern GetTokenPattern(int index)
		{
			return Parser.TokenPatterns[index];
		}

		/// <summary>
		/// Advances the parser context to use for this and child elements.
		/// </summary>
		/// <remarks>
		/// For <paramref name="context"/> it updates the settings to use as local element. <br/>
		/// It makes a <paramref name="childContext"/> that have advanced recursion depth and settings for child elements.
		/// </remarks>
		public void AdvanceContext(ref ParserContext context, out ParserContext childContext)
		{
			childContext = context;

			if (Settings.isDefault)
			{
				childContext.settings = context.settings;
			}
			else
			{
				context.settings.Resolve(Settings, Parser.Settings, out var forLocal, out var forChildren);
				context.settings = forLocal;

				childContext.settings = forChildren;
			}

			childContext.recursionDepth++;
		}

		/// <summary>
		/// Records an error associated with this element in the provided parser context.
		/// </summary>
		/// <param name="context">The parser context to record the error in.</param>
		protected void RecordError(ParserContext context)
		{
			context.RecordError(null, Id, this is TokenPattern);
		}

		/// <summary>
		/// Records an error associated with this element and the specific message in the provided parser context.
		/// </summary>
		/// <param name="context">The parser context to record the error in.</param>
		/// <param name="position">The position in the input string where the error occurred.</param>
		protected void RecordError(ParserContext context, int position)
		{
			context.RecordError(position, null, Id, this is TokenPattern);
		}

		/// <summary>
		/// Records an error associated with this element and the specific message in the provided parser context.
		/// </summary>
		/// <param name="context">The parser context to record the error in.</param>
		/// <param name="message">The error message to record.</param>
		protected void RecordError(ParserContext context, string? message)
		{
			context.RecordError(message, Id, this is TokenPattern);
		}

		/// <summary>
		/// Records an error associated with this element and the specific message in the provided parser context.
		/// </summary>
		/// <param name="context">The parser context to record the error in.</param>
		/// <param name="position">The position in the input string where the error occurred.</param>
		/// <param name="message">The error message to record.</param>
		protected void RecordError(ParserContext context, int position, string? message)
		{
			context.RecordError(position, message, Id, this is TokenPattern);
		}

		/// <summary>
		/// Tries to parse a rule with the given ID using the specified parsing context.
		/// </summary>
		/// <param name="ruleId">The ID of the rule to parse.</param>
		/// <param name="context">The parsing context to use for the parse operation.</param>
		/// <param name="parsedRule">The output parameter to store the parsed rule. If parsing fails, this will be set to a failure rule.</param>
		/// <returns><see langword="true"/> if parsing was successful; otherwise, <see langword="false"/> if parsing failed.</returns>
		protected bool TryParseRule(int ruleId, ParserContext context, out ParsedRule parsedRule)
		{
			return Parser.TryParseRule(ruleId, context, out parsedRule);
		}

		/// <summary>
		/// Tries to parse a token pattern with the given ID using the specified parsing context.
		/// </summary>
		/// <param name="ruleId">The ID of the token pattern to parse.</param>
		/// <param name="context">The parsing context to use for the parse operation.</param>
		/// <returns>The parsed rule result.</returns>
		protected ParsedRule ParseRule(int ruleId, ParserContext context)
		{
			return Parser.ParseRule(ruleId, context);
		}

		/// <summary>
		/// Tries to match a token with the given ID using the specified parsing context.
		/// </summary>
		/// <param name="tokenId">The ID of the token to match.</param>
		/// <param name="context">The parsing context to use for the match operation.</param>
		/// <param name="parsedToken">The output parameter to store the parsed token. If matching fails, this will be set to a failure token.</param>
		/// <returns><see langword="true"/> if matching was successful; otherwise, <see langword="false"/> if matching failed.</returns>
		protected bool TryMatchToken(int tokenId, ParserContext context, out ParsedToken parsedToken)
		{
			return Parser.TryMatchToken(tokenId, context, out parsedToken);
		}

		public override string ToString()
		{
			if (Aliases.Count > 0)
				return $"'{Aliases[0]}'";
			return ToString(2); // Default depth is 2.
		}

		public override bool Equals(object? obj)
		{
			return obj is ParserElement other &&
				Id == other.Id &&
				ReferenceEquals(Parser, other.Parser) &&
				Aliases.SequenceEqual(other.Aliases) &&
				Settings == other.Settings;
		}

		public override int GetHashCode()
		{
			int hashCode = 17;
			hashCode ^= Id.GetHashCode() * 23;
			hashCode ^= (Parser?.GetHashCode() ?? 0) * 27;
			hashCode ^= Aliases.GetSequenceHashCode() * 29;
			hashCode ^= Settings.GetHashCode() * 31;
			return hashCode;
		}
	}
}