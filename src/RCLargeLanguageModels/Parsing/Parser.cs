using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using RCLargeLanguageModels.Parsing.ParserRules;

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

			foreach (var rule in rules)
				rule.InitializeInternal();

			foreach (var pattern in tokenPatterns)
				pattern.InitializeInternal();
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
		private ParsedRule ParseNonValidated(ParserRule rule, int ruleId,
			ref ParserContext context, ref ParserContext childContext)
		{
			switch (context.settings.caching)
			{
				case ParserCachingMode.Default:
					return rule.Parse(context, childContext);

				case ParserCachingMode.CacheAll:
					if (!context.cache.TryGetRule(ruleId, context.position, out var parsedRule))
					{
						parsedRule = rule.Parse(context, childContext);
						context.cache.AddRule(ruleId, context.position, parsedRule);
					}
					return parsedRule;

				case ParserCachingMode.TokenPatterns:
					if (rule is TokenParserRule)
					{
						if (!context.cache.TryGetRule(ruleId, context.position, out parsedRule))
						{
							parsedRule = rule.Parse(context, childContext);
							context.cache.AddRule(ruleId, context.position, parsedRule);
						}
						return parsedRule;
					}
					return rule.Parse(context, childContext);

				case ParserCachingMode.Rules:
					if (rule is not TokenParserRule)
					{
						if (!context.cache.TryGetRule(ruleId, context.position, out parsedRule))
						{
							parsedRule = rule.Parse(context, childContext);
							context.cache.AddRule(ruleId, context.position, parsedRule);
						}
						return parsedRule;
					}
					return rule.Parse(context, childContext);

				default:
					throw new InvalidOperationException("Invalid caching mode.");
			}
		}

		/// <summary>
		/// Tries to skip the skip-rule specified in settings.
		/// </summary>
		private bool TrySkip(ref ParserContext context, ref ParserContext childContext)
		{
			int skipRuleId = context.settings.skipRule;
			int startIndex = context.position;

			if (skipRuleId != -1 && context.position < context.str.Length &&
				!context.skippedPositions[context.position])
			{
				var skipRule = Rules[skipRuleId];

				var ctx = context;
				skipRule.AdvanceContext(ref ctx, out var childCtx);

				ctx.settings.skipRule = -1;
				childCtx.settings.skipRule = -1;

				int skipRuleLength = 0;

				do
				{
					var parsedSkipRule = ParseNonValidated(skipRule, skipRuleId,
						ref ctx, ref childCtx);

					skipRuleLength = parsedSkipRule.length;
					if (parsedSkipRule.success)
					{
						int newPosition = parsedSkipRule.startIndex + parsedSkipRule.length;
						context.position = childContext.position = ctx.position = childCtx.position = newPosition;
						context.skippedRules.Add(parsedSkipRule);
					}

					context.skippedPositions[context.position] = true;
				}
				while (skipRuleLength > 0);
			}

			return startIndex != context.position;
		}

		/// <summary>
		/// Creates a <see cref="ParsingException"/> from the current parser context.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private ParsingException ExceptionFromContext(ParserContext context)
		{
			var errors = context.errors.ToArray();

			if (errors.Length == 0)
				return new ParsingException(context, "Unknown error.");

			return new ParsingException(context, errors);
		}

		/// <summary>
		/// Parses the given input using the specified rule identifier and parser context.
		/// </summary>
		/// <remarks>
		/// Throws a <see cref="ParsingException"/> if parsing fails.
		/// </remarks>
		/// <param name="ruleId">The unique identifier for the parser rule to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <returns>A parsed rule object containing the result of the parse.</returns>
		internal ParsedRule ParseRule(int ruleId, ParserContext context)
		{
			var parsedRule = TryParseRule(ruleId, context);
			if (parsedRule.success)
				return parsedRule;

			throw ExceptionFromContext(context);
		}

		/// <summary>
		/// Tries to parse the given input using the specified rule identifier and parser context.
		/// </summary>
		/// <param name="ruleId">The unique identifier for the parser rule to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <returns>A parsed rule object containing the result of the parse.</returns>
		internal ParsedRule TryParseRule(int ruleId, ParserContext context)
		{
			if (CheckRecursionDepth(context))
				throw new ParsingException(context, "Maximum recursion depth exceeded.");

			var rule = Rules[ruleId];
			rule.AdvanceContext(ref context, out var childContext);

			switch (context.settings.skippingStrategy)
			{
				case ParserSkippingStrategy.Default:

					var parsedRule = ParseNonValidated(rule, ruleId, ref context, ref childContext);
					if (parsedRule.success)
						context.successPositions[parsedRule.startIndex] = true;
					return parsedRule;

				case ParserSkippingStrategy.SkipBeforeParsing:

					TrySkip(ref context, ref childContext);

					parsedRule = ParseNonValidated(rule, ruleId, ref context, ref childContext);
					if (parsedRule.success && parsedRule.startIndex < context.str.Length)
						context.successPositions[parsedRule.startIndex] = true;
					return parsedRule;

				case ParserSkippingStrategy.TryParseThenSkip:

					parsedRule = ParseNonValidated(rule, ruleId, ref context, ref childContext);
					if (parsedRule.success)
					{
						if (parsedRule.startIndex < context.str.Length)
							context.successPositions[parsedRule.startIndex] = true;
						return parsedRule;
					}

					TrySkip(ref context, ref childContext);

					parsedRule = ParseNonValidated(rule, ruleId, ref context, ref childContext);
					if (parsedRule.success && parsedRule.startIndex < context.str.Length)
						context.successPositions[parsedRule.startIndex] = true;
					return parsedRule;

				default:
					throw new ParsingException(context, "Invalid skipping strategy.");
			}
		}

		/// <summary>
		/// Matches the given input using the specified token identifier and parser context.
		/// </summary>
		/// <returns>A parsed token object containing the result of the match.</returns>
		internal ParsedElement MatchToken(int tokenPatternId, string input, int position)
		{
			var tokenPattern = TokenPatterns[tokenPatternId];
			return tokenPattern.Match(input, position);
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

			var context = new ParserContext(this, input);
			var parsedRule = ParseRule(ruleId, context);
			return new ParsedRuleResult(ParseTreeOptimization.None, null, context, parsedRule);
		}

		/// <summary>
		/// Tries to parse a rule using the specified rule alias and input text.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="result">The parsed rule containing the result of the parse.</param>
		/// <returns>True if a rule was parsed successfully, false otherwise.</returns>
		public bool TryParseRule(string ruleAlias, string input, out ParsedRuleResult result)
		{
			if (!_rulesAliases.TryGetValue(ruleAlias, out var ruleId))
				throw new ArgumentException("Invalid rule alias", nameof(ruleAlias));

			var context = new ParserContext(this, input);
			var parsedRule = TryParseRule(ruleId, context);
			result = new ParsedRuleResult(ParseTreeOptimization.None, null, context, parsedRule);
			return parsedRule.success;
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// </summary>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		public ParsedTokenResult MatchToken(string tokenPatternAlias, string input)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var context = new ParserContext(this, input);
			var parsedToken = MatchToken(tokenPatternId, context.str, context.position);
			return new ParsedTokenResult(null, context, parsedToken);
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// </summary>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="result">The parsed token containing the result of the parse.</param>
		/// <returns>True if a token was matched, false otherwise.</returns>
		public bool TryMatchToken(string tokenPatternAlias, string input, out ParsedTokenResult result)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var context = new ParserContext(this, input);
			var parsedToken = MatchToken(tokenPatternId, context.str, context.position);
			result = new ParsedTokenResult(null, context, parsedToken);
			return parsedToken.success;
		}
	}
}