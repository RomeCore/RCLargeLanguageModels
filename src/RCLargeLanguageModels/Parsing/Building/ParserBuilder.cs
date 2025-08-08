using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
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
			Dictionary<string, BuildableParserRule> namedRules = new();
			Dictionary<BuildableParserRule, int> rules = new();
			Dictionary<BuildableParserRule, (List<BuildableParserRule>?, List<BuildableTokenPattern>?)> rulesArgMap = new();
			
			int tokenCounter = 0;
			Dictionary<string, BuildableTokenPattern> namedTokenPatterns = new();
			Dictionary<BuildableTokenPattern, int> tokenPatterns = new();
			Dictionary<BuildableTokenPattern, List<BuildableTokenPattern>?> tokenPatternsArgMap = new();
			
			Queue<BuildableParserRule> rulesToProcess = new();
			Queue<BuildableTokenPattern> tokensToProcess = new();

			foreach (var rule in _rules)
			{
				if (!rule.Value.CanBeBuilt)
					throw new ParserBuildingException($"Rule '{rule.Key}' cannot be built, because it's empty.");
				if (rule.Value.BuildingRule.Value.VariantIndex != 1)
					throw new ParserBuildingException($"Rule '{rule.Key}' cannot be built, because it directly points " +
						$"to another rule (single name reference).");

				var brule = rule.Value.BuildingRule.Value.Value2;
				rulesToProcess.Enqueue(brule);
				namedRules.Add(rule.Key, brule);
			}

			foreach (var pattern in _tokenPatterns)
			{
				if (!pattern.Value.CanBeBuilt)
					throw new ParserBuildingException($"Token pattern '{pattern.Key}' cannot be built, because it's empty.");
				if (pattern.Value.BuildingPattern.Value.VariantIndex != 1)
					throw new ParserBuildingException($"Token pattern '{pattern.Key}' cannot be built, because it directly points " +
						$"to another token pattern (single name reference).");

				var bpattern = pattern.Value.BuildingPattern.Value.Value2;
				tokensToProcess.Enqueue(bpattern);
				namedTokenPatterns.Add(pattern.Key, bpattern);
			}

			while (rulesToProcess.Count > 0)
			{
				var rule = rulesToProcess.Dequeue();
				if (rules.ContainsKey(rule))
					continue;

				rules.Add(rule, ruleCounter++);

				List<BuildableParserRule> childrenToArgs = null;
				List<BuildableTokenPattern> tokenChildrenToArgs = null;

				var children = rule.Children;
				if (children != null)
				{
					childrenToArgs = new();
					foreach (var child in children)
					{
						child.Switch(name =>
						{
							if (namedRules.TryGetValue(name, out var namedRule))
								childrenToArgs.Add(namedRule);
							else
								throw new ParserBuildingException($"Unknown rule reference '{name}'.");
						}, brule =>
						{
							childrenToArgs.Add(brule);
							rulesToProcess.Enqueue(brule);
						});
					}
				}

				var tokenChildren = rule.TokenChildren;
				if (tokenChildren != null)
				{
					tokenChildrenToArgs = new();
					foreach (var tokenChild in tokenChildren)
					{
						tokenChild.Switch(name =>
						{
							if (namedTokenPatterns.TryGetValue(name, out var namedPattern))
								tokenChildrenToArgs.Add(namedPattern);
							else
								throw new ParserBuildingException($"Unknown token pattern reference '{name}'.");
						}, bpattern =>
						{
							tokenChildrenToArgs.Add(bpattern);
							tokensToProcess.Enqueue(bpattern);
						});
					}
				}

				rulesArgMap[rule] = (childrenToArgs, tokenChildrenToArgs);
			}

			while (tokensToProcess.Count > 0)
			{
				var pattern = tokensToProcess.Dequeue();
				if (tokenPatterns.ContainsKey(pattern))
					continue;

				tokenPatterns.Add(pattern, tokenCounter++);

				List<BuildableTokenPattern> childrenToArgs = null;

				var children = pattern.Children;
				if (children != null)
				{
					childrenToArgs = new();
					foreach (var child in children)
					{
						child.Switch(name =>
						{
							if (namedTokenPatterns.TryGetValue(name, out var namedPattern))
								childrenToArgs.Add(namedPattern);
							else
								throw new ParserBuildingException($"Unknown rule reference '{name}'.");
						}, bpattern =>
						{
							childrenToArgs.Add(bpattern);
							tokensToProcess.Enqueue(bpattern);
						});
					}
				}

				tokenPatternsArgMap[pattern] = childrenToArgs;
			}

			ParserRule[] resultRules = new ParserRule[rules.Count];

			foreach (var rule in rules)
			{
				var (children, tokenChildren) = rulesArgMap[rule.Key];

				var builtRule = rule.Key.Build(
					children?.Select(r => rules[r]).ToList(),
					tokenChildren?.Select(p => tokenPatterns[p]).ToList());

				builtRule.Id = rule.Value;

				resultRules[rule.Value] = builtRule;
			}

			foreach (var rule in namedRules)
			{
				var index = rules[rule.Value];
				resultRules[index].Alias = rule.Key;
			}

			TokenPattern[] resultTokenPatterns = new TokenPattern[tokenPatterns.Count];

			foreach (var tokenPattern in tokenPatterns)
			{
				var children = tokenPatternsArgMap[tokenPattern.Key];

				var builtTokenPattern = tokenPattern.Key.Build(
					children?.Select(p => tokenPatterns[p]).ToList());

				builtTokenPattern.Id = tokenPattern.Value;

				resultTokenPatterns[tokenPattern.Value] = builtTokenPattern;
			}

			foreach (var pattern in namedTokenPatterns)
			{
				var index = tokenPatterns[pattern.Value];
				resultTokenPatterns[index].Alias = pattern.Key;
			}

			return new Parser(resultTokenPatterns.ToImmutableArray(), resultRules.ToImmutableArray());
		}
	}
}