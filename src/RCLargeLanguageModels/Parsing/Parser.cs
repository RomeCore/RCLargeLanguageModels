using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace RCLargeLanguageModels.Parsing
{
	/// <summary>
	/// Represents a parser for parsing text data into AST.
	/// </summary>
	public class Parser
	{
		private readonly Dictionary<string, int> _tokenPatternsAliases = new Dictionary<string, int>();
		private readonly Dictionary<string, int> _rulesAliases = new Dictionary<string, int>();

		/// <summary>
		/// Gets the token patterns used by this parser.
		/// </summary>
		public ImmutableArray<TokenPattern> TokenPatterns { get; }

		/// <summary>
		/// Gets the rules used by this parser.
		/// </summary>
		public ImmutableArray<ParserRule> Rules { get; }

		/// <summary>
		/// Gets the global settings used by this parser.
		/// </summary>
		public ParserSettings Settings { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Parser"/> class.
		/// </summary>
		/// <param name="tokenPatterns">The token patterns to use. </param>
		/// <param name="rules">The rules to use.</param>
		/// <param name="settings">The settings to use.</param>
		public Parser(ImmutableArray<TokenPattern> tokenPatterns, ImmutableArray<ParserRule> rules, ParserSettings settings)
		{
			Rules = rules;
			TokenPatterns = tokenPatterns;
			Settings = settings;

			foreach (var rule in rules)
			{
				if (rule.Parser != null)
					throw new InvalidOperationException("Parser already set for a rule.");
				rule.Parser = this;

				foreach (var alias in rule.Aliases)
				{
					if (_rulesAliases.ContainsKey(alias))
						throw new InvalidOperationException("Alias already used by another rule.");
					_rulesAliases.Add(alias, rule.Id);
				}
			}

			foreach (var pattern in tokenPatterns)
			{
				if (pattern.Parser != null)
					throw new InvalidOperationException("Parser already set for a token pattern.");
				pattern.Parser = this;

				foreach (var alias in pattern.Aliases)
				{
					if (_tokenPatternsAliases.ContainsKey(alias))
						throw new InvalidOperationException("Alias already used by another token pattern.");
					_tokenPatternsAliases.Add(alias, pattern.Id);
				}
			}
		}

		/// <summary>
		/// Gets a token pattern by its alias.
		/// </summary>
		/// <param name="alias">The alias of the token pattern.</param>
		/// <returns>The token pattern with the specified alias.</returns>
		/// <exception cref="InvalidOperationException">Thrown if no token pattern is found with the specified alias.</exception>
		public TokenPattern GetTokenPattern(string alias)
		{
			if (_tokenPatternsAliases.TryGetValue(alias, out var id))
				return TokenPatterns[id];
			throw new InvalidOperationException($"Token pattern not found with alias '{alias}'.");
		}

		/// <summary>
		/// Gets a rule by its alias.
		/// </summary>
		/// <param name="alias">The alias of the rule.</param>
		/// <returns>The rule with the specified alias.</returns>
		/// <exception cref="InvalidOperationException">Thrown if no rule is found with the specified alias.</exception>
		public ParserRule GetRule(string alias)
		{
			if (_rulesAliases.TryGetValue(alias, out var id))
				return Rules[id];
			throw new InvalidOperationException($"Rule not found with alias '{alias}'.");
		}

		/// <summary>
		/// Creates a new parser context for the given input.
		/// </summary>
		/// <param name="input">The input string to parse.</param>
		/// <returns>A new parser context for the given input.</returns>
		public ParserContext CreateContext(string input)
		{
			return new ParserContext(this, input);
		}

		/// <summary>
		/// Returns true if the current recursion depth is greater than the maximum allowed recursion depth.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool CheckRecursionDepth(ParserContext context)
		{
			if (context.settings.maxRecursionDepth != 0 && context.recursionDepth > context.settings.maxRecursionDepth)
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Tries to parse rule based on the caching settings.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool TryParseNonValidated(int ruleId, ParserContext context, out ParsedRule parsedRule)
		{
			var rule = Rules[ruleId];

			if (context.settings.caching == ParserCachingMode.Default ||
				context.settings.caching == ParserCachingMode.Rules)
			{
				if (!context.cache.TryGetRule(ruleId, context.position, out parsedRule))
				{
					rule.TryParse(context, out parsedRule);
					context.cache.AddRule(ruleId, context.position, parsedRule);
				}
			}
			else
			{
				rule.TryParse(context, out parsedRule);
			}

			if (parsedRule.success)
				context.successPositions.Add(context.position);

			return parsedRule.success;
		}

		/// <summary>
		/// Parses rule based on the caching settings.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private ParsedRule ParseNonValidated(int ruleId, ParserContext context)
		{
			var rule = Rules[ruleId];

			ParsedRule parsedRule;

			if (context.settings.caching == ParserCachingMode.Default ||
				context.settings.caching == ParserCachingMode.Rules)
			{
				if (!context.cache.TryGetRule(ruleId, context.position, out parsedRule))
				{
					parsedRule = rule.Parse(context);
					context.cache.AddRule(ruleId, context.position, parsedRule);
				}
			}
			else
			{
				parsedRule = rule.Parse(context);
			}

			if (parsedRule.success)
				context.successPositions.Add(context.position);
			else
				throw new ParsingException("Parse failed with unknown reason.", context.str, context.position);

			return parsedRule;
		}

		/// <summary>
		/// Tries to match token based on the caching settings.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool TryMatchNonValidated(int tokenPatternId, ParserContext context, out ParsedToken parsedToken)
		{
			var token = TokenPatterns[tokenPatternId];

			if (context.settings.caching == ParserCachingMode.Default ||
				context.settings.caching == ParserCachingMode.TokenPatterns)
			{
				if (!context.cache.TryGetToken(tokenPatternId, context.position, out parsedToken))
				{
					token.TryMatch(context, out parsedToken);
					context.cache.AddToken(tokenPatternId, context.position, parsedToken);
				}
			}
			else
			{
				token.TryMatch(context, out parsedToken);
			}

			if (parsedToken.success)
				context.successPositions.Add(context.position);

			return parsedToken.success;
		}

		/// <summary>
		/// Tries to skip the skip-rule specified in settings.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void TrySkip(ref ParserContext context)
		{
			if (context.settings.skipRule != -1)
			{
				TryParseNonValidated(context.settings.skipRule, context, out var parsedSkipRule);

				if (parsedSkipRule.success)
				{
					context.position = parsedSkipRule.startIndex + parsedSkipRule.length;
					context.skippedRules.Add(parsedSkipRule);
				}
			}
		}

		/// <summary>
		/// Parses the given input using the specified rule identifier and parser context.
		/// </summary>
		/// <param name="ruleId">The unique identifier for the parser rule to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <returns>A parsed rule object containing the result of the parse.</returns>
		internal ParsedRule ParseRule(int ruleId, ParserContext context)
		{
			if (CheckRecursionDepth(context))
				throw new ParsingException("Maximum recursion depth exceeded.", context.str, context.position);

			TrySkip(ref context);

			return ParseNonValidated(ruleId, context);
		}

		/// <summary>
		/// Tries to parse a rule using the specified rule identifier and parser context.
		/// </summary>
		/// <param name="ruleId">The unique identifier for the parser rule to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="parsedRule">The parsed rule object containing the result of the parse.</param>
		/// <returns>True if a rule was parsed successfully, false otherwise.</returns>
		internal bool TryParseRule(int ruleId, ParserContext context, out ParsedRule parsedRule)
		{
			if (CheckRecursionDepth(context))
			{
				context.RecordError("Maximum recursion depth exceeded.");
				parsedRule = ParsedRule.Fail;
				return false;
			}

			TrySkip(ref context);

			return TryParseNonValidated(ruleId, context, out parsedRule);
		}

		/// <summary>
		/// Parses the given input using the specified token pattern.
		/// </summary>
		/// <param name="tokenPatternId">The unique identifier for the token pattern to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="parsedToken">The parsed token object containing the result of the parse.</param>
		/// <returns>True if a token was matched, false otherwise.</returns>
		internal bool TryMatchToken(int tokenPatternId, ParserContext context, out ParsedToken parsedToken)
		{
			if (CheckRecursionDepth(context))
			{
				context.RecordError("Maximum recursion depth exceeded.");
				parsedToken = ParsedToken.Fail;
				return false;
			}

			return TryMatchNonValidated(tokenPatternId, context, out parsedToken);
		}

		/// <summary>
		/// Parses the given input using the specified rule alias and parser context.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <returns>A parsed rule containing the result of the parse.</returns>
		public ParsedRuleResult ParseRule(string ruleAlias, ParserContext context)
		{
			if (!_rulesAliases.TryGetValue(ruleAlias, out var ruleId))
				throw new ArgumentException("Invalid rule alias", nameof(ruleAlias));

			return new ParsedRuleResult(null, context, ParseRule(ruleId, context));
		}

		/// <summary>
		/// Tries to parse a rule using the specified rule alias and parser context.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="result">The parsed rule containing the result of the parse.</param>
		/// <returns>True if a rule was parsed successfully, false otherwise.</returns>
		public bool TryParseRule(string ruleAlias, ParserContext context, out ParsedRuleResult result)
		{
			if (!_rulesAliases.TryGetValue(ruleAlias, out var ruleId))
				throw new ArgumentException("Invalid rule alias", nameof(ruleAlias));

			var res = TryParseRule(ruleId, context, out var parsedRule);
			result = new ParsedRuleResult(null, context, parsedRule);
			return res;
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias.
		/// </summary>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="result">The parsed token containing the result of the parse.</param>
		/// <returns>True if a token was matched, false otherwise.</returns>
		public bool TryMatchToken(string tokenPatternAlias, ParserContext context, out ParsedTokenResult result)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var res = TryMatchToken(tokenPatternId, context, out var parsedToken);
			result = new ParsedTokenResult(null, context, parsedToken);
			return res;
		}



		/// <summary>
		/// Parses the given input using the specified rule alias and input text.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <returns>A parsed rule containing the result of the parse.</returns>
		public ParsedRuleResult ParseRule(string ruleAlias, string input)
		{
			if (!_rulesAliases.TryGetValue(ruleAlias, out var ruleId))
				throw new ArgumentException("Invalid rule alias", nameof(ruleAlias));

			var context = CreateContext(input);
			return new ParsedRuleResult(null, context, ParseRule(ruleId, context));
		}
	}
}