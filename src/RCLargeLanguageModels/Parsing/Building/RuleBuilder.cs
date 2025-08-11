using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RCLargeLanguageModels.Parsing.Building.ParserRules;
using RCLargeLanguageModels.Parsing.Building.TokenPatterns;
using RCLargeLanguageModels.Parsing.TokenPatterns;

namespace RCLargeLanguageModels.Parsing.Building
{
	/// <summary>
	/// Represents a builder for constructing rules that are used in parsing processes.
	/// </summary>
	public class RuleBuilder : ParserElementBuilder<RuleBuilder>
	{
		private Or<string, BuildableParserRule>? _rule;

		/// <summary>
		/// Gets the rule being built.
		/// </summary>
		public Or<string, BuildableParserRule>? BuildingRule => _rule;

		public override bool CanBeBuilt => _rule.HasValue;
		protected override RuleBuilder GetThis() => this;
		public override RuleBuilder AddToken(Or<string, BuildableTokenPattern> childToken)
		{
			return AddRule(new BuildableTokenParserRule
			{
				Child = childToken
			});
		}

		/// <summary>
		/// Adds a token (name or child pattern) to the current sequence with the parsed value factory.
		/// </summary>
		/// <param name="childToken">The token to add. Can be a name or a child pattern.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder AddToken(Or<string, BuildableTokenPattern> childToken,
			Func<ParsedRuleResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddRule(new BuildableTokenParserRule
			{
				Child = childToken,
				ParsedValueFactory = parsedValueFactory
			});
		}

		/// <summary>
		/// Adds a rule to the current sequence.
		/// </summary>
		/// <param name="childRule">The rule to add. Can be a name or a child pattern.</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder AddRule(Or<string, BuildableParserRule> childRule)
		{
			if (!_rule.HasValue)
			{
				_rule = childRule;
			}
			else if (_rule.Value.VariantIndex == 1 &&
					_rule.Value.AsT2() is BuildableSequenceParserRule sequenceRule)
			{
				sequenceRule.Elements.Add(childRule);
			}
			else
			{
				var newSequence = new BuildableSequenceParserRule();
				newSequence.Elements.Add(_rule.Value);
				newSequence.Elements.Add(childRule);
				_rule = newSequence;
			}
			return this;
		}

		/// <summary>
		/// Adds a rule to the current sequence.
		/// </summary>
		/// <param name="childRule">The rule to add. Can be a name or a child pattern.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder AddRule(BuildableParserRule childRule,
			Func<ParsedRuleResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			childRule.ParsedValueFactory = parsedValueFactory;
			config?.Invoke(childRule.Settings);
			return AddRule(new Or<string, BuildableParserRule>(childRule));
		}

		/// <summary>
		/// Adds a rule to the current sequence.
		/// </summary>
		/// <param name="ruleName">The name of the rule to add.</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder Rule(string ruleName)
		{
			return AddRule(ruleName);
		}

		/// <summary>
		/// Adds a token to the current sequence.
		/// </summary>
		/// <param name="tokenName">The name of the token to add.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder Token(string tokenName, Func<ParsedRuleResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddRule(new BuildableTokenParserRule
			{
				Child = tokenName
			}, parsedValueFactory, config);
		}

		/// <summary>
		/// Adds a token to the current sequence.
		/// </summary>
		/// <param name="token">The token pattern to add.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder Token(TokenPattern token, Func<ParsedRuleResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddRule(new BuildableTokenParserRule
			{
				Child = new BuildableLeafTokenPattern
				{
					TokenPattern = token
				}
			}, parsedValueFactory, config);
		}

		/// <summary>
		/// Adds a token to the current sequence.
		/// </summary>
		/// <param name="builderAction">The token pattern builder.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public RuleBuilder Token(Action<TokenBuilder> builderAction, Func<ParsedRuleResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			var builder = new TokenBuilder();
			builderAction(builder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Builder action did not add any tokens.");

			return AddRule(new BuildableTokenParserRule
			{
				Child = builder.BuildingPattern.Value
			}, parsedValueFactory, config);
		}

		/// <summary>
		/// Converts the pattern into a sequence if it is not already one.
		/// </summary>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder ToSequence()
		{
			if (!_rule.HasValue)
			{
				throw new ParserBuildingException("Cannot convert empty rule to sequence.");
			}
			else if (_rule.Value.VariantIndex != 1 ||
					_rule.Value.AsT2() is not BuildableSequenceParserRule)
			{
				var newSequence = new BuildableSequenceParserRule();
				newSequence.Elements.Add(_rule.Value);
				_rule = newSequence;
			}
			return this;
		}

		/// <summary>
		/// Sets the transformation function to the current sequence rule.
		/// </summary>
		/// <remarks>
		/// This method should be called after adding at least two child elements to the sequence.
		/// </remarks>
		/// <param name="parsedValueFactory">The transformation function (parsed value factory) to set.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the current rule is not a sequence or has fewer than two child elements.</exception>
		public RuleBuilder Transform(Func<ParsedRuleResult, object?>? parsedValueFactory)
		{
			if (_rule?.AsT2() is BuildableSequenceParserRule sequenceRule)
				sequenceRule.ParsedValueFactory = parsedValueFactory;
			else
				throw new ParserBuildingException("Parsed value factory can only be set on a sequence rule " +
					"(must be added at least two child elements or must be converted to a sequence first).");
			return this;
		}

		/// <summary>
		/// Configures the local settings for the current sequence rule.
		/// </summary>
		/// <remarks>
		/// This method should be called after adding at least two child elements to the sequence.
		/// </remarks>
		/// <param name="configAction">The configuration action.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the current rule is not a sequence or has fewer than two child elements.</exception>
		public RuleBuilder Configure(Action<ParserLocalSettingsBuilder> configAction)
		{
			if (_rule?.AsT2() is BuildableSequenceParserRule sequenceRule)
				configAction(sequenceRule.Settings);
			else
				throw new ParserBuildingException("Only a sequence rule can be configured " +
					"(must be added at least two child elements or must be converted to a sequence first).");
			return this;
		}

		/// <summary>
		/// Adds an optional rule to the current sequence.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the child rule.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the builder action did not add any elements.</exception>
		public RuleBuilder Optional(Action<RuleBuilder> builderAction, Func<ParsedRuleResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			var builder = new RuleBuilder();
			builderAction(builder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Optional child rule cannot be empty.");

			return AddRule(new BuildableOptionalParserRule
			{
				Child = builder.BuildingRule.Value
			}, parsedValueFactory, config);
		}

		/// <summary>
		/// Adds a repeatable rule to the current sequence with specified minimum and maximum occurrences.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the repeatable rule.</param>
		/// <param name="min">The minimum number of times the rule can be repeated.</param>
		/// <param name="max">The maximum number of times the rule can be repeated. -1 indicates no upper limit.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the builder action did not add any elements.</exception>
		public RuleBuilder Repeat(Action<RuleBuilder> builderAction, int min, int max, Func<ParsedRuleResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			var builder = new RuleBuilder();
			builderAction(builder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Repeated child rule cannot be empty.");

			return AddRule(new BuildableRepeatParserRule
			{
				MinCount = min,
				MaxCount = max,
				Child = builder.BuildingRule.Value
			}, parsedValueFactory, config);
		}

		/// <summary>
		/// Adds a repeatable rule to the current sequence with specified minimum occurrences.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the repeatable rule.</param>
		/// <param name="min">The minimum number of times the rule can be repeated.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the builder action did not add any elements.</exception>
		public RuleBuilder Repeat(Action<RuleBuilder> builderAction, int min, Func<ParsedRuleResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return Repeat(builderAction, min, -1, parsedValueFactory, config);
		}

		/// <summary>
		/// Adds a repeatable rule to the current sequence that matches zero or more occurrences of the child rule.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the repeatable rule.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the builder action did not add any elements.</exception>
		public RuleBuilder ZeroOrMore(Action<RuleBuilder> builderAction, Func<ParsedRuleResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return Repeat(builderAction, 0, -1, parsedValueFactory, config);
		}

		/// <summary>
		/// Adds a repeatable rule to the current sequence that matches one or more occurrences of the child rule.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the repeatable rule.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the builder action did not add any elements.</exception>
		public RuleBuilder OneOrMore(Action<RuleBuilder> builderAction, Func<ParsedRuleResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return Repeat(builderAction, 1, -1, parsedValueFactory, config);
		}

		/// <summary>
		/// Adds a choice rule to the current sequence.
		/// </summary>
		/// <param name="choices">The choices for this rule.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public RuleBuilder Choice(IEnumerable<Or<Action<RuleBuilder>, string>> choices, Func<ParsedRuleResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			var builtValues = choices.Select(c =>
			{
				if (c.VariantIndex == 0)
				{
					var builder = new RuleBuilder();
					c.AsT1().Invoke(builder);
					if (!builder.CanBeBuilt)
						throw new ParserBuildingException("Choice child rule cannot be empty.");
					return builder.BuildingRule.Value;
				}
				else
				{
					var name = c.AsT2();
					return new Or<string, BuildableParserRule>(name);
				}
			}).ToList();

			var choice = new BuildableChoiceParserRule();
			choice.Choices.AddRange(builtValues);
			return AddRule(choice, parsedValueFactory, config);
		}

		/// <summary>
		/// Adds a choice rule to the current sequence.
		/// </summary>
		/// <param name="choices">The choices for this rule.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public RuleBuilder Choice(IEnumerable<Action<RuleBuilder>> choices, Func<ParsedRuleResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return Choice(choices.Select(c => new Or<Action<RuleBuilder>, string>(c)).ToArray(),
				parsedValueFactory, config);
		}

		/// <summary>
		/// Adds a choice rule to the current sequence.
		/// </summary>
		/// <param name="choices">The choices for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public RuleBuilder Choice(params Action<RuleBuilder>[] choices)
		{
			return Choice(choices, null, null);
		}

		/// <summary>
		/// Adds a separated repeatable rule to the current sequence with specified minimum and maximum occurrences.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the repeatable rule.</param>
		/// <param name="separatorBuilderAction">The rule builder action to build the separator rule.</param>
		/// <param name="min">The minimum number of times the rule can be repeated.</param>
		/// <param name="max">The maximum number of times the rule can be repeated. -1 indicates no upper limit.</param>
		/// <param name="allowTrailingSeparator">Whether to allow a trailing separator.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of the builder actions did not add any elements.</exception>
		public RuleBuilder SeparatedRepeat(Action<RuleBuilder> builderAction, Action<RuleBuilder> separatorBuilderAction,
			int min, int max, bool allowTrailingSeparator = false, Func<ParsedRuleResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			RuleBuilder builder = new(), separatorBuilder = new();
			builderAction(builder);
			separatorBuilderAction(separatorBuilder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Repeated child rule cannot be empty.");

			if (!separatorBuilder.CanBeBuilt)
				throw new ParserBuildingException("Separator child rule cannot be empty.");

			return AddRule(new BuildableSeparatedRepeatParserRule
			{
				MinCount = min,
				MaxCount = max,
				Child = builder.BuildingRule.Value,
				Separator = separatorBuilder.BuildingRule.Value,
				AllowTrailingSeparator = allowTrailingSeparator
			}, parsedValueFactory, config);
		}

		/// <summary>
		/// Adds a separated repeatable rule to the current sequence with specified minimum occurrences.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the repeatable rule.</param>
		/// <param name="separatorBuilderAction">The rule builder action to build the separator rule.</param>
		/// <param name="min">The minimum number of times the rule can be repeated.</param>
		/// <param name="allowTrailingSeparator">Whether to allow a trailing separator.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of the builder actions did not add any elements.</exception>
		public RuleBuilder SeparatedRepeat(Action<RuleBuilder> builderAction, Action<RuleBuilder> separatorBuilderAction,
			int min, bool allowTrailingSeparator = false, Func<ParsedRuleResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return SeparatedRepeat(builderAction, separatorBuilderAction, min, -1, allowTrailingSeparator,
				parsedValueFactory, config);
		}

		/// <summary>
		/// Adds a separated repeatable rule to the current sequence that matches zero or more occurrences of the child rule.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the repeatable rule.</param>
		/// <param name="separatorBuilderAction">The rule builder action to build the separator rule.</param>
		/// <param name="allowTrailingSeparator">Whether to allow a trailing separator.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of the builder actions did not add any elements.</exception>
		public RuleBuilder ZeroOrMoreSeparated(Action<RuleBuilder> builderAction, Action<RuleBuilder> separatorBuilderAction,
			bool allowTrailingSeparator = false, Func<ParsedRuleResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return SeparatedRepeat(builderAction, separatorBuilderAction, 0, -1, allowTrailingSeparator,
				parsedValueFactory, config);
		}

		/// <summary>
		/// Adds a separated repeatable rule to the current sequence that matches one or more occurrences of the child rule.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the repeatable rule.</param>
		/// <param name="separatorBuilderAction">The rule builder action to build the separator rule.</param>
		/// <param name="allowTrailingSeparator">Whether to allow a trailing separator.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of the builder actions did not add any elements.</exception>
		public RuleBuilder OneOrMoreSeparated(Action<RuleBuilder> builderAction, Action<RuleBuilder> separatorBuilderAction,
			bool allowTrailingSeparator = false, Func<ParsedRuleResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return SeparatedRepeat(builderAction, separatorBuilderAction, 1, -1, allowTrailingSeparator,
				parsedValueFactory, config);
		}
	}
}