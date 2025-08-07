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
	public class RuleBuilder
	{
		private BuildableParserRule? _rule;

		/// <summary>
		/// Gets the rule being built.
		/// </summary>
		public BuildableParserRule? BuildingRule => _rule;

		public RuleBuilder Add(BuildableParserRule childRule)
		{
			if (_rule == null)
			{
				_rule = childRule;
			}
			else if (_rule is BuildableSequenceParserRule sequenceRule)
			{
				sequenceRule.Elements.Add(childRule);
			}
			else
			{
				var newSequence = new BuildableSequenceParserRule();
				newSequence.Elements.Add(_rule);
				newSequence.Elements.Add(childRule);
				_rule = newSequence;
			}
			return this;
		}

		public RuleBuilder Add(string ruleName)
		{
			if (_rule is BuildableSequenceParserRule sequenceRule)
			{
				sequenceRule.Elements.Add(ruleName);
			}
			else
			{
				var newSequence = new BuildableSequenceParserRule();
				newSequence.Elements.Add(ruleName);
				_rule = newSequence;
			}
			return this;
		}

		public RuleBuilder Rule(string ruleName)
		{
			return Add(ruleName);
		}

		public RuleBuilder Token(string tokenName, Func<ParsedToken, object?>? parsedValueFactory = null)
		{
			return Add(new BuildableTokenParserRule
			{
				Child = tokenName,
				ParsedValueFactory = parsedValueFactory
			});
		}

		public RuleBuilder Token(TokenPattern token, Func<ParsedToken, object?>? parsedValueFactory = null)
		{
			return Add(new BuildableTokenParserRule
			{
				Child = new BuildableLeafTokenPattern
				{
					TokenPattern = token
				},
				ParsedValueFactory = parsedValueFactory
			});
		}

		public RuleBuilder Token(Action<TokenBuilder> builderAction, Func<ParsedToken, object?>? parsedValueFactory = null)
		{
			var builder = new TokenBuilder();
			builderAction(builder);

			if (builder.BuildingPattern == null)
				throw new ParserBuildingException("Builder action did not add any tokens.");

			return Add(new BuildableTokenParserRule
			{
				Child = builder.BuildingPattern,
				ParsedValueFactory = parsedValueFactory
			});
		}

		public RuleBuilder SetParsedValueFactory(Func<List<ParsedRule>, object?>? parsedValueFactory)
		{
			if (_rule is BuildableSequenceParserRule sequencePattern)
				sequencePattern.ParsedValueFactory = parsedValueFactory;
			else
				throw new ParserBuildingException("Parsed value factory can only be set on a sequence rule (must be added at least one named element or two child elements first).");
			return this;
		}

		public RuleBuilder Optional(Action<RuleBuilder> builderAction, Func<ParsedRule?, object?>? parsedValueFactory = null)
		{
			var builder = new RuleBuilder();
			builderAction(builder);

			if (builder.BuildingRule == null)
				throw new ParserBuildingException("Optional child rule cannot be empty.");

			return Add(new BuildableOptionalParserRule
			{
				Child = builder.BuildingRule,
				ParsedValueFactory = parsedValueFactory
			});
		}

		public RuleBuilder Repeat(int min, int max, Action<RuleBuilder> builderAction, Func<List<ParsedRule>, object?>? parsedValueFactory = null)
		{
			var builder = new RuleBuilder();
			builderAction(builder);

			if (builder.BuildingRule == null)
				throw new ParserBuildingException("Repeated child rule cannot be empty.");

			return Add(new BuildableRepeatParserRule
			{
				MinCount = min,
				MaxCount = max,
				Child = builder.BuildingRule,
				ParsedValueFactory = parsedValueFactory
			});
		}

		public RuleBuilder Repeat(int min, Action<RuleBuilder> builderAction, Func<List<ParsedRule>, object?>? parsedValueFactory = null)
		{
			return Repeat(min, -1, builderAction, parsedValueFactory);
		}

		public RuleBuilder ZeroOrMore(Action<RuleBuilder> builderAction, Func<List<ParsedRule>, object?>? parsedValueFactory = null)
		{
			return Repeat(0, -1, builderAction, parsedValueFactory);
		}

		public RuleBuilder OneOrMore(Action<RuleBuilder> builderAction, Func<List<ParsedRule>, object?>? parsedValueFactory = null)
		{
			return Repeat(1, -1, builderAction, parsedValueFactory);
		}

		public RuleBuilder Choice(params Or<Action<RuleBuilder>, string>[] choices)
		{
			return Choice(null, choices);
		}

		public RuleBuilder Choice(params Action<RuleBuilder>[] choices)
		{
			return Choice(null, choices);
		}

		public RuleBuilder Choice(Func<ParsedRule, object?>? parsedValueFactory, params Or<Action<RuleBuilder>, string>[] choices)
		{
			var builtValues = choices.Select(c =>
			{
				if (c.VariantIndex == 0)
				{
					var builder = new RuleBuilder();
					c.AsT1().Invoke(builder);
					if (builder.BuildingRule == null)
						throw new ParserBuildingException("Choice child rule cannot be empty.");
					return new Or<string, BuildableParserRule>(builder.BuildingRule);
				}
				else
				{
					var name = c.AsT2();
					return new Or<string, BuildableParserRule>(name);
				}
			}).ToList();

			var choice = new BuildableChoiceParserRule();
			choice.Choices.AddRange(builtValues);
			choice.ParsedValueFactory = parsedValueFactory;

			return Add(choice);
		}

		public RuleBuilder Choice(Func<ParsedRule, object?>? parsedValueFactory, params Action<RuleBuilder>[] choices)
		{
			return Choice(parsedValueFactory, choices.Select(c => new Or<Action<RuleBuilder>, string>(c)).ToArray());
		}

		public RuleBuilder Literal(string literal, Func<string, object?>? parsedValueFactory = null)
		{
			return Token(new LiteralTokenPattern(literal, parsedValueFactory));
		}

		public RuleBuilder Literal(string literal, StringComparer comparer, Func<string, object?>? parsedValueFactory = null)
		{
			return Token(new LiteralTokenPattern(literal, comparer, parsedValueFactory));
		}

		public RuleBuilder Regex(string regex, RegexOptions options = RegexOptions.Compiled, Func<Match, object?>? parsedValueFactory = null)
		{
			return Token(new RegexTokenPattern(regex, parsedValueFactory, options));
		}

		public RuleBuilder Regex(string regex, Func<Match, object?>? parsedValueFactory)
		{
			return Token(new RegexTokenPattern(regex, parsedValueFactory));
		}
	}
}