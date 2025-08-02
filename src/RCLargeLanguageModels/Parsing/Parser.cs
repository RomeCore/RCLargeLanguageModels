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
		private ImmutableArray<TokenPattern> _tokenPatterns;
		private Dictionary<int, ParserRule>? _rulesDict = new Dictionary<int, ParserRule>();
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
		/// Adds a token pattern to the parser.
		/// </summary>
		/// <param name="patternId">The unique identifier for the token pattern.</param>
		/// <param name="pattern">The token pattern to add.</param>
		public void AddTokenPattern(int patternId, TokenPattern pattern)
		{
			if (_initialized)
				throw new InvalidOperationException("Cannot add token patterns after initialization");
			if (_tokenPatternsDict.ContainsKey(patternId))
				throw new InvalidOperationException("Duplicate token pattern ID");
			if (patternId > 1024)
				throw new InvalidOperationException("Token pattern ID too large");
			_tokenPatternsDict.Add(patternId, pattern);
		}

		/// <summary>
		/// Adds a parser rule to the parser.
		/// </summary>
		/// <param name="ruleId">The unique identifier for the parser rule.</param>
		/// <param name="rule">The parser rule to add.</param>
		public void AddParserRule(int ruleId, ParserRule rule)
		{
			if (_initialized)
				throw new InvalidOperationException("Cannot add parser rules after initialization");
			if (_rulesDict.ContainsKey(ruleId))
				throw new InvalidOperationException("Duplicate parser rule ID");
			if (ruleId > 1024)
				throw new InvalidOperationException("Rule ID too large");
			_rulesDict.Add(ruleId, rule);
		}

		/// <summary>
		/// Initializes the parser. This method must be called before parsing can begin.
		/// </summary>
		public void Initialize()
		{
			if (_initialized)
				throw new InvalidOperationException("Parser is already initialized.");

			var maxPatternId = _tokenPatternsDict.Keys.Max();
			var maxRuleId = _rulesDict.Keys.Max();

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
				throw new InvalidOperationException("Parser must be initialized before parsing rules");
			if (ruleId < 0 || ruleId >= _rules.Length || _rules[ruleId] is not ParserRule rule)
				throw new ArgumentException("Invalid rule ID", nameof(ruleId));

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
		/// Tries to parse a rule using the specified rule identifier and parser context.
		/// </summary>
		/// <param name="ruleId">The unique identifier for the parser rule to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="parsedRule">The parsed rule object containing the result of the parse.</param>
		/// <returns>True if a rule was parsed successfully, false otherwise.</returns>
		public bool TryParseRule(int ruleId, ParserContext context, out ParsedRule parsedRule)
		{
			if (!_initialized)
				throw new InvalidOperationException("Parser must be initialized before parsing rules");
			if (ruleId < 0 || ruleId >= _rules.Length || _rules[ruleId] is not ParserRule rule)
				throw new ArgumentException("Invalid rule ID", nameof(ruleId));

			var position = context.position;
			if (context.cache.TryGetRule(ruleId, position, out parsedRule))
				return parsedRule.success;

			var success = rule.TryParse(context, out parsedRule);
			if (success != parsedRule.success)
				throw new InvalidOperationException("Parsed token and match success fact mismatch");
			context.cache.AddRule(ruleId, position, parsedRule);

			return parsedRule.success;
		}

		/// <summary>
		/// Parses the given input using the specified token pattern.
		/// </summary>
		/// <param name="tokenId">The unique identifier for the token pattern to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="parsedToken">The parsed token object containing the result of the parse.</param>
		/// <returns>True if a token was matched, false otherwise.</returns>
		public bool TryMatchToken(int tokenId, ParserContext context, out ParsedToken parsedToken)
		{
			if (!_initialized)
				throw new InvalidOperationException("Parser must be initialized before parsing rules");
			if (tokenId < 0 || tokenId >= _tokenPatterns.Length || _tokenPatterns[tokenId] is not TokenPattern pattern)
				throw new ArgumentException("Invalid token pattern ID", nameof(tokenId));

			var position = context.position;
			if (context.cache.TryGetToken(tokenId, position, out parsedToken))
				return parsedToken.success;

			var success = pattern.TryMatch(context, out parsedToken);
			if (success != parsedToken.success)
				throw new InvalidOperationException("Parsed token and match success fact mismatch");
			context.cache.AddToken(tokenId, position, parsedToken);

			return parsedToken.success;
		}
	}
}