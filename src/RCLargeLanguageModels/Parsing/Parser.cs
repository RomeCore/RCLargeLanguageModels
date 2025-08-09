using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Text;

namespace RCLargeLanguageModels.Parsing
{
	/// <summary>
	/// Represents a parser for parsing text data into AST.
	/// </summary>
	public class Parser
	{
		private readonly Dictionary<string, int> _tokenPatternsAliases = new Dictionary<string, int>();
		private readonly ImmutableArray<TokenPattern> _tokenPatterns;

		private readonly Dictionary<string, int> _rulesAliases = new Dictionary<string, int>();
		private readonly ImmutableArray<ParserRule> _rules;

		/// <summary>
		/// Gets the token patterns used by this parser.
		/// </summary>
		public ImmutableArray<TokenPattern> TokenPatterns => _tokenPatterns;

		/// <summary>
		/// Gets the rules used by this parser.
		/// </summary>
		public ImmutableArray<ParserRule> Rules => _rules;

		/// <summary>
		/// Initializes a new instance of the <see cref="Parser"/> class.
		/// </summary>
		/// <param name="tokenPatterns">The token patterns to use.</param>
		/// <param name="rules">The rules to use.</param>
		internal Parser(ImmutableArray<TokenPattern> tokenPatterns, ImmutableArray<ParserRule> rules)
		{
			_rules = rules;
			_tokenPatterns = tokenPatterns;

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
						throw new InvalidOperationException("Alias already used by another rule.");
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
				return _tokenPatterns[id];
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
				return _rules[id];
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
		/// Parses the given input using the specified rule identifier and parser context.
		/// </summary>
		/// <param name="ruleId">The unique identifier for the parser rule to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <returns>A parsed rule object containing the result of the parse.</returns>
		internal ParsedRule ParseRule(int ruleId, ParserContext context)
		{
			var rule = _rules[ruleId];

			context.SkipWhiteSpace();

			var position = context.position;
			if (context.cache.TryGetRule(ruleId, position, out var parsedRule))
				return parsedRule;

			parsedRule = rule.Parse(context);
			context.cache.AddRule(ruleId, position, parsedRule);
			if (!parsedRule.success)
				throw new ParsingException("Parse failed", context.str, position);

			return parsedRule;
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
		/// Tries to parse a rule using the specified rule identifier and parser context.
		/// </summary>
		/// <param name="ruleId">The unique identifier for the parser rule to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="parsedRule">The parsed rule object containing the result of the parse.</param>
		/// <returns>True if a rule was parsed successfully, false otherwise.</returns>
		internal bool TryParseRule(int ruleId, ParserContext context, out ParsedRule parsedRule)
		{
			var rule = _rules[ruleId];

			context.SkipWhiteSpace();

			var position = context.position;
			if (context.cache.TryGetRule(ruleId, position, out parsedRule))
				return parsedRule.success;

			var success = rule.TryParse(context, out parsedRule);
			context.cache.AddRule(ruleId, position, parsedRule);

			return parsedRule.success;
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
		/// Parses the given input using the specified token pattern.
		/// </summary>
		/// <param name="tokenPatternId">The unique identifier for the token pattern to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="parsedToken">The parsed token object containing the result of the parse.</param>
		/// <returns>True if a token was matched, false otherwise.</returns>
		internal bool TryMatchToken(int tokenPatternId, ParserContext context, out ParsedToken parsedToken)
		{
			var token = _tokenPatterns[tokenPatternId];

			var position = context.position;
			if (context.cache.TryGetToken(tokenPatternId, position, out parsedToken))
				return parsedToken.success;

			var success = token.TryMatch(context, out parsedToken);
			context.cache.AddToken(tokenPatternId, position, parsedToken);

			return parsedToken.success;
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
	}
}