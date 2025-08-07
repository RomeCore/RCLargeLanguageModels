using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RCLargeLanguageModels.Parsing.Building.ParserRules;
using RCLargeLanguageModels.Parsing.Building.TokenPatterns;
using RCLargeLanguageModels.Parsing.ParserRules;

namespace RCLargeLanguageModels.Parsing.Building
{
	/* Concept:
	
	var builder = new ParserBuilder();
 
	var identifier_token = builder.CreateToken("identifier").Regex("[a-zA-Z_][a-zA-Z0-9_]*");
	var number_token = builder.CreateToken("number", str => double.Parse(str)).Regex("[0-9]+");
	var esc_text_token = builder.CreateToken("esc_text").EscapedText(charSet: "{}@", EscapingStrategy.DoubleCharacters);
 
	var template_rule = builder.CreateRule("template")
		.Literal("@").Literal("template").Token("identifier").Literal("{").Rule("template_content").Literal("}");

	var template_content_rule = builder.CreateRule("template_content", RuleOptions.KeepWhiteSpaces)
		.Choice(
			b => b.Literal("@").Rule("expression").ForseSkipWhiteSpaces(),
			b => b.Token("esc_text")
		);

	var expression_rule = builder.CreateRule("expression")
		.Choice(
			b => b.Rule("expression").LiteralChoice("+", "-").Rule("expression"),
			b => b.Rule("expression").LiteralChoice("*", "/").Rule("expression"),
			b => b.Literal("(").Rule("expression").Literal(")"),
			b => b.Token("number")
		);

	var parser = builder.Build();
	 */

	/// <summary>
	/// Represents a builder for constructing parsers.
	/// </summary>
	public class ParserBuilder
	{
		private readonly Dictionary<string, TokenBuilder> _tokenPatterns = new Dictionary<string, TokenBuilder>();
		private readonly Dictionary<string, RuleBuilder> _rules = new Dictionary<string, RuleBuilder>();

		/// <summary>
		/// Initializes a new instance of the <see cref="ParserBuilder"/> class.
		/// </summary>
		public ParserBuilder()
		{
		}

		/// <summary>
		/// Creates a token pattern builder and registers it under the given name.
		/// </summary>
		/// <param name="name">The name of the token pattern. Will be bound to token pattern as alias in the built parser.</param>
		/// <returns>A <see cref="TokenBuilder"/> instance for building the token pattern.</returns>
		/// <exception cref="ArgumentException">Thrown if a token pattern with the same name already exists.</exception>
		public TokenBuilder CreateToken(string name)
		{
			if (_tokenPatterns.ContainsKey(name))
				throw new ArgumentException($"Token with name '{name}' already exists.");

			var token = new TokenBuilder();
			_tokenPatterns[name] = token;
			return token;
		}

		/// <summary>
		/// Creates a rule builder and registers it under the given name.
		/// </summary>
		/// <param name="name">The name of the rule. Will be bound to rule as alias in the built parser.</param>
		/// <returns>A <see cref="RuleBuilder"/> instance for building the rule.</returns>
		/// <exception cref="ArgumentException">Thrown if a rule with the same name already exists.</exception>
		public RuleBuilder CreateRule(string name)
		{
			if (_rules.ContainsKey(name))
				throw new ArgumentException($"Rule with name '{name}' already exists.");

			var rule = new RuleBuilder();
			_rules[name] = rule;
			return rule;
		}

		/// <summary>
		/// Builds the parser from the registered token patterns and rules.
		/// </summary>
		/// <returns>A <see cref="Parser"/> instance representing the built parser.</returns>
		public Parser Build()
		{
			int ruleCounter = 0;
			Dictionary<BuildableParserRule, int> rules = new Dictionary<BuildableParserRule, int>();
			Dictionary<BuildableParserRule, List<BuildableParserRule>> rulesArgMap = new Dictionary<BuildableParserRule, List<BuildableParserRule>>();
			
			int tokenCounter = 0;
			Dictionary<BuildableTokenPattern, int> tokenPatterns = new Dictionary<BuildableTokenPattern, int>();
			Dictionary<BuildableTokenPattern, List<BuildableTokenPattern>> tokenPatternsArgMap = new Dictionary<BuildableTokenPattern, List<BuildableTokenPattern>>();
			
			Stack<BuildableParserRule> ruleStack = new Stack<BuildableParserRule>();
			Stack<BuildableTokenPattern> tokenStack = new Stack<BuildableTokenPattern>();

			foreach (var rule in _rules.Values)
			{
				if (rule.BuildingRule == null)
					throw new ParserBuildingException("Rule cannot be null.");
				ruleStack.Push(rule.BuildingRule);
			}
			foreach (var pattern in _tokenPatterns.Values)
			{
				if (pattern.BuildingPattern == null)
					throw new ParserBuildingException("Token pattern cannot be null.");
				tokenStack.Push(pattern.BuildingPattern);
			}

			while (ruleStack.Count > 0)
			{
				var rule = ruleStack.Pop();
				if (rules.ContainsKey(rule))
					continue;

				rules.Add(rule, ruleCounter++);

				if (rule is BuildableTokenParserRule tokenRule)
				{
					if (tokenRule.Child.VariantIndex == 0)
					{
						if (_tokenPatterns.TryGetValue(tokenRule.Child.AsT1(), out var tb))
							tokenStack.Push(tb.BuildingPattern);
						else
							throw new ParserBuildingException($"Unknown token pattern '{tokenRule.Child.AsT1()}'.");
					}
					else
						tokenStack.Push(tokenRule.Child.AsT2());
				}
				else
				{
					var children = rule.Children?.Select(o =>
					{
						if (o.VariantIndex == 0)
						{
							if (_rules.TryGetValue(o.AsT1(), out var rb))
								return rb.BuildingRule;
							else
								throw new ParserBuildingException($"Unknown rule '{o.AsT1()}'.");
						}
						else
							return o.AsT2();
					}).ToList();

					foreach (var child in children)
						ruleStack.Push(child);
					rulesArgMap.Add(rule, children);
				}
			}

			while (tokenStack.Count > 0)
			{
				var pattern = tokenStack.Pop();
				if (tokenPatterns.ContainsKey(pattern))
					continue;

				tokenPatterns.Add(pattern, tokenCounter++);

				if (pattern is BuildableLeafTokenPattern leafToken)
				{
					// Skip leaf token patterns for now.
				}
				else
				{
					var children = pattern.Children?.Select(o =>
					{
						if (o.VariantIndex == 0)
						{
							if (_tokenPatterns.TryGetValue(o.AsT1(), out var tb))
								return tb.BuildingPattern;
							else
								throw new ParserBuildingException($"Unknown token pattern '{o.AsT1()}'.");
						}
						else
							return o.AsT2();
					}).ToList();

					foreach (var child in children)
						tokenStack.Push(child);
					tokenPatternsArgMap.Add(pattern, children);
				}
			}

			var parser = new Parser();

			foreach (var rule in rules)
			{
				if (rule.Key is BuildableTokenParserRule tokenRule)
				{
					var pattern = tokenRule.Child.Match(v1 =>
					{
						return _tokenPatterns[v1].BuildingPattern;
					}, v2 =>
					{
						return v2;
					});
					var index = tokenPatterns[pattern];
					parser.AddParserRule(rule.Value, tokenRule.Build(new List<int> { index }));
				}
				else
				{
					var indices = rulesArgMap[rule.Key].Select(r => rules[r]).ToList();
					parser.AddParserRule(rule.Value, rule.Key.Build(indices));
				}
			}

			foreach (var rule in _rules)
			{
				var index = rules[rule.Value.BuildingRule];
				parser.AddParserRuleAlias(rule.Key, index);
			}

			foreach (var pattern in tokenPatterns)
			{
				if (pattern.Key is BuildableLeafTokenPattern leafToken)
				{
					parser.AddTokenPattern(pattern.Value, leafToken.Build(null));
				}
				else
				{
					var indices = tokenPatternsArgMap[pattern.Key].Select(p => tokenPatterns[p]).ToList();
					parser.AddTokenPattern(pattern.Value, pattern.Key.Build(indices));
				}
			}

			foreach (var pattern in _tokenPatterns)
			{
				var index = tokenPatterns[pattern.Value.BuildingPattern];
				parser.AddTokenPatternAlias(pattern.Key, index);
			}

			parser.Initialize();
			return parser;
		}
	}
}