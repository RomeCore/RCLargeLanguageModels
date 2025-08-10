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
		private readonly ParserSettingsBuilder _settingsBuilder = new ParserSettingsBuilder();

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
		/// Gets the current settings builder for configuring additional options.
		/// </summary>
		/// <returns>A <see cref="ParserSettingsBuilder"/> instance for configuring additional options.</returns>
		public ParserSettingsBuilder Settings()
		{
			return _settingsBuilder;
		}

		/// <summary>
		/// Builds the parser from the registered token patterns and rules.
		/// </summary>
		/// <returns>A <see cref="Parser"/> instance representing the built parser.</returns>
		public Parser Build()
		{
			// Counters for assigning unique IDs to rules and tokens
			int ruleCounter = 0;
			int tokenCounter = 0;

			// Maps for named rules and tokens to their buildable representations
			Dictionary<string, BuildableParserRule> namedRules = new();
			Dictionary<string, BuildableTokenPattern> namedTokenPatterns = new();

			// Maps for buildable rules and tokens to their assigned integer IDs
			Dictionary<BuildableParserElement, int> elements = new();

			// Maps for rules and tokens to their children dependencies
			Dictionary<BuildableParserElement, (List<BuildableParserRule>?, List<BuildableTokenPattern>?)> argMap = new();

			// Queues to process rules and tokens breadth-first
			Queue<BuildableParserElement> elementsToProcess = new();

			// Initialize processing queue and namedRules map with root rules
			foreach (var rule in _rules)
			{
				if (!rule.Value.CanBeBuilt)
					throw new ParserBuildingException($"Rule '{rule.Key}' cannot be built, because it's empty.");

				// Detect circular references and resolve to the base rule
				HashSet<string> checkedNames = new HashSet<string>();
				var currentRule = rule.Value.BuildingRule.Value;

				while (currentRule.VariantIndex != 1)
				{
					if (!checkedNames.Add(currentRule.Value1))
						throw new ParserBuildingException($"Circular reference detected in rule '{rule.Key}': " +
							$"{string.Join(" -> ", checkedNames.Append(currentRule.Value1).Select(n => $"'{n}'"))}");

					if (_rules.TryGetValue(currentRule.Value1, out var nextRule))
					{
						if (!nextRule.CanBeBuilt)
							throw new ParserBuildingException($"Rule '{currentRule.Value1}' cannot be built, because it's empty.");

						currentRule = nextRule.BuildingRule.Value;
					}
					else
					{
						throw new ParserBuildingException($"Rule '{currentRule.Value1}' cannot be found.");
					}
				}

				var brule = currentRule.Value2;
				elementsToProcess.Enqueue(brule);
				namedRules.Add(rule.Key, brule);
			}

			// Initialize processing queue and namedTokenPatterns map with root token patterns
			foreach (var pattern in _tokenPatterns)
			{
				if (!pattern.Value.CanBeBuilt)
					throw new ParserBuildingException($"Token pattern '{pattern.Key}' cannot be built, because it's empty.");

				// Detect circular references and resolve to the base token pattern
				HashSet<string> checkedNames = new HashSet<string>();
				var currentPattern = pattern.Value.BuildingPattern.Value;

				while (currentPattern.VariantIndex != 1)
				{
					if (!checkedNames.Add(currentPattern.Value1))
						throw new ParserBuildingException($"Circular reference detected in token pattern '{pattern.Key}': " +
							$"{string.Join(" -> ", checkedNames.Append(currentPattern.Value1).Select(n => $"'{n}'"))}");

					if (_tokenPatterns.TryGetValue(currentPattern.Value1, out var nextPattern))
					{
						if (!nextPattern.CanBeBuilt)
							throw new ParserBuildingException($"Token pattern '{currentPattern.Value1}' cannot be built, because it's empty.");

						currentPattern = nextPattern.BuildingPattern.Value;
					}
					else
					{
						throw new ParserBuildingException($"Token pattern '{currentPattern.Value1}' cannot be found.");
					}
				}

				var bpattern = currentPattern.Value2;
				elementsToProcess.Enqueue(bpattern);
				namedTokenPatterns.Add(pattern.Key, bpattern);
			}

			// Process all rules in the queue, assign IDs and collect dependencies
			while (elementsToProcess.Count > 0)
			{
				// Register a rule ID if it's not already registered
				var element = elementsToProcess.Dequeue();
				if (elements.ContainsKey(element))
					continue;

				if (element is BuildableParserRule rule)
					elements.Add(element, ruleCounter++);
				else if (element is BuildableTokenPattern pattern)
					elements.Add(element, tokenCounter++);

				List<BuildableParserRule> childrenToArgs = null;
				List<BuildableTokenPattern> tokenChildrenToArgs = null;

				// Queue child rules for processing
				var children = element.RuleChildren;
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
							elementsToProcess.Enqueue(brule);
						});
					}
				}

				// Queue child token patterns for processing
				var tokenChildren = element.TokenChildren;
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
							elementsToProcess.Enqueue(bpattern);
						});
					}
				}

				// Register rule dependencies for rule building
				argMap[element] = (childrenToArgs, tokenChildrenToArgs);
			}

			// Build final ParserRule instances with resolved children and assign their IDs
			ParserRule[] resultRules = new ParserRule[elements.Count(e => e.Key is BuildableParserRule)];
			TokenPattern[] resultTokenPatterns = new TokenPattern[elements.Count(e => e.Key is BuildableTokenPattern)];

			foreach (var elem in elements)
			{
				var element = elem.Key;
				var id = elem.Value;

				var (children, tokenChildren) = argMap[element];

				var builtElement = element.Build(
					children?.Select(r => elements[r]).ToList(),
					tokenChildren?.Select(p => elements[p]).ToList());

				builtElement.Id = id;

				if (builtElement is ParserRule rule)
					resultRules[id] = rule;
				else if (builtElement is TokenPattern pattern)
					resultTokenPatterns[id] = pattern;
			}

			// Add aliases to the built rules for name lookup
			foreach (var rule in namedRules)
			{
				var index = elements[rule.Value];
				resultRules[index].Aliases = resultRules[index].Aliases.Add(rule.Key);
			}

			// Add aliases to the built token patterns for name lookup
			foreach (var pattern in namedTokenPatterns)
			{
				var index = elements[pattern.Value];
				resultTokenPatterns[index].Aliases = resultTokenPatterns[index].Aliases.Add(pattern.Key);
			}

			// Return the fully built parser instance with rules and token patterns
			return new Parser(resultTokenPatterns.ToImmutableArray(), resultRules.ToImmutableArray());
		}
	}
}