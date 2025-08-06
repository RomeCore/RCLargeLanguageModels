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
	/// <remarks>
	/// When initializing the parser, you must provide a set of rules and token patterns that define how to parse the text data. <br/>
	/// When parsing the data inside the rules and tokens, you need to use the provided methods by <see cref="Parser"/> class to handle the parsing process.
	/// </remarks>
	public class Parser
	{
		private bool _initialized = false;

		private Dictionary<int, TokenPattern>? _tokenPatternsDict = new Dictionary<int, TokenPattern>();
		private Dictionary<string, int>? _tokenPatternsAliases = new Dictionary<string, int>();
		private ImmutableArray<TokenPattern> _tokenPatterns;

		private Dictionary<int, ParserRule>? _rulesDict = new Dictionary<int, ParserRule>();
		private Dictionary<string, int>? _rulesAliases = new Dictionary<string, int>();
		private ImmutableArray<ParserRule> _rules;

		/// <summary>
		/// Gets the token patterns used by this parser. Cannot be retrieved until initialization.
		/// </summary>
		public ImmutableArray<TokenPattern> TokenPatterns => _initialized ? _tokenPatterns : throw new InvalidOperationException("Parser is not initialized.");

		/// <summary>
		/// Gets the rules used by this parser. Cannot be retrieved until initialization.
		/// </summary>
		public ImmutableArray<ParserRule> Rules => _initialized ? _rules : throw new InvalidOperationException("Parser is not initialized.");

		/// <summary>
		/// Adds a token pattern to the parser. Will not work after initialization.
		/// </summary>
		/// <param name="patternId">The unique identifier for the token pattern.</param>
		/// <param name="pattern">The token pattern to add.</param>
		public void AddTokenPattern(int patternId, TokenPattern pattern)
		{
			if (_initialized)
				throw new InvalidOperationException("Cannot add token patterns after initialization");
			if (_tokenPatternsDict.ContainsKey(patternId))
				throw new InvalidOperationException("Duplicate token pattern ID");
			if (patternId > 8096)
				throw new InvalidOperationException("Token pattern ID too large");
			_tokenPatternsDict.Add(patternId, pattern);
		}

		/// <summary>
		/// Adds an alias for a token pattern.
		/// </summary>
		/// <param name="alias">The alias to add.</param>
		/// <param name="tokenPatternId">The ID of the token pattern to which the alias should be added.</param>
		/// <exception cref="InvalidOperationException">Thrown if the alias already exists.</exception>
		public void AddTokenPatternAlias(string alias, int tokenPatternId)
		{
			if (_tokenPatternsAliases.ContainsKey(alias))
				throw new InvalidOperationException("Duplicate alias");
			_tokenPatternsAliases.Add(alias, tokenPatternId);
		}

		/// <summary>
		/// Adds a parser rule to the parser. Will not work after initialization.
		/// </summary>
		/// <param name="ruleId">The unique identifier for the parser rule.</param>
		/// <param name="rule">The parser rule to add.</param>
		public void AddParserRule(int ruleId, ParserRule rule)
		{
			if (_initialized)
				throw new InvalidOperationException("Cannot add parser rules after initialization");
			if (_rulesDict.ContainsKey(ruleId))
				throw new InvalidOperationException("Duplicate parser rule ID");
			if (ruleId > 8096)
				throw new InvalidOperationException("Rule ID too large");
			_rulesDict.Add(ruleId, rule);
		}

		/// <summary>
		/// Adds an alias for a parser rule.
		/// </summary>
		/// <param name="alias">The alias to add.</param>
		/// <param name="parserRuleId">The ID of the parser rule to which the alias should be added.</param>
		/// <exception cref="InvalidOperationException">Thrown if the alias already exists.</exception>
		public void AddParserRuleAlias(string alias, int parserRuleId)
		{
			if (_rulesAliases.ContainsKey(alias))
				throw new InvalidOperationException("Duplicate alias");
			_rulesAliases.Add(alias, parserRuleId);
		}

		/// <summary>
		/// Initializes the parser. This method must be called before parsing can begin.
		/// </summary>
		public void Initialize()
		{
			if (_initialized)
				throw new InvalidOperationException("Parser is already initialized.");

			var maxPatternId = _tokenPatternsDict.Count == 0 ? 0 : _tokenPatternsDict.Keys.Max();
			var maxRuleId = _rulesDict.Count == 0 ? 0 : _rulesDict.Keys.Max();

			var tokenPatternsArray = ImmutableArray.CreateBuilder<TokenPattern>(maxPatternId + 1);
			var rulesArray = ImmutableArray.CreateBuilder<ParserRule>(maxRuleId + 1);

			foreach (var tokenPattern in _tokenPatternsDict)
				tokenPatternsArray[tokenPattern.Key] = tokenPattern.Value;
			_tokenPatterns = tokenPatternsArray.ToImmutable();

			foreach (var rule in _rulesDict)
				rulesArray[rule.Key] = rule.Value;
			_rules = rulesArray.ToImmutable();

			_tokenPatternsDict = null;
			_rulesDict = null;
			_initialized = true;
		}

		/// <summary>
		/// Creates a new parser context for the given input.
		/// </summary>
		/// <param name="input">The input string to parse.</param>
		/// <returns>A new parser context for the given input.</returns>
		public ParserContext CreateContext(string input)
		{
			if (!_initialized)
				throw new InvalidOperationException("Parser must be initialized before creating a context");
			return new ParserContext(this, input);
		}

		/// <summary>
		/// Parses the given input using the specified rule identifier and parser context.
		/// </summary>
		/// <param name="ruleId">The unique identifier for the parser rule to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <returns>A parsed rule object containing the result of the parse.</returns>
		public ParsedRule ParseRule(int ruleId, ParserContext context)
		{
			if (!_initialized)
				throw new InvalidOperationException("Parser must be initialized before parsing");
			if (ruleId < 0 || ruleId >= _rules.Length || _rules[ruleId] is not ParserRule rule)
				throw new ArgumentException("Invalid rule ID", nameof(ruleId));

			var position = context.position;
			if (context.cache.TryGetRule(ruleId, position, out var parsedRule))
				return parsedRule;

			parsedRule = rule.Parse(ruleId, context);
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
		/// <returns>A parsed rule object containing the result of the parse.</returns>
		public ParsedRule ParseRule(string ruleAlias, ParserContext context)
		{
			if (!_rulesAliases.TryGetValue(ruleAlias, out var ruleId))
				throw new ArgumentException("Invalid rule alias", nameof(ruleAlias));
			return ParseRule(ruleAlias, context);
		}

		/// <summary>
		/// Tries to parse a rule using the specified rule identifier and parser context.
		/// </summary>
		/// <param name="ruleId">The unique identifier for the parser rule to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="parsedRule">The parsed rule object containing the result of the parse.</param>
		/// <returns>True if a rule was parsed successfully, false otherwise.</returns>
		public bool TryParseRule(int ruleId, ParserContext context, out ParsedRule parsedRule)
		{
			if (!_initialized)
				throw new InvalidOperationException("Parser must be initialized before parsing");
			if (ruleId < 0 || ruleId >= _rules.Length || _rules[ruleId] is not ParserRule rule)
				throw new ArgumentException("Invalid rule ID", nameof(ruleId));

			var position = context.position;
			if (context.cache.TryGetRule(ruleId, position, out parsedRule))
				return parsedRule.success;

			var success = rule.TryParse(ruleId, context, out parsedRule);
			context.cache.AddRule(ruleId, position, parsedRule);

			return parsedRule.success;
		}

		/// <summary>
		/// Tries to parse a rule using the specified rule alias and parser context.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="parsedRule">The parsed rule object containing the result of the parse.</param>
		/// <returns>True if a rule was parsed successfully, false otherwise.</returns>
		public bool TryParseRule(string ruleAlias, ParserContext context, out ParsedRule parsedRule)
		{
			if (!_rulesAliases.TryGetValue(ruleAlias, out var ruleId))
				throw new ArgumentException("Invalid rule alias", nameof(ruleAlias));
			return TryParseRule(ruleAlias, context, out parsedRule);
		}

		/// <summary>
		/// Parses the given input using the specified token pattern.
		/// </summary>
		/// <param name="tokenPatternId">The unique identifier for the token pattern to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="parsedToken">The parsed token object containing the result of the parse.</param>
		/// <returns>True if a token was matched, false otherwise.</returns>
		public bool TryMatchToken(int tokenPatternId, ParserContext context, out ParsedToken parsedToken)
		{
			if (!_initialized)
				throw new InvalidOperationException("Parser must be initialized before parsing");
			if (tokenPatternId < 0 || tokenPatternId >= _tokenPatterns.Length || _tokenPatterns[tokenPatternId] is not TokenPattern pattern)
				throw new ArgumentException("Invalid token pattern ID", nameof(tokenPatternId));

			var position = context.position;
			if (context.cache.TryGetToken(tokenPatternId, position, out parsedToken))
				return parsedToken.success;

			var success = pattern.TryMatch(tokenPatternId, context, out parsedToken);
			context.cache.AddToken(tokenPatternId, position, parsedToken);

			return parsedToken.success;
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias.
		/// </summary>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="parsedToken">The parsed token object containing the result of the parse.</param>
		/// <returns>True if a token was matched, false otherwise.</returns>
		public bool TryMatchToken(string tokenPatternAlias, ParserContext context, out ParsedToken parsedToken)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));
			return TryMatchToken(tokenPatternAlias, context, out parsedToken);
		}
	}
}