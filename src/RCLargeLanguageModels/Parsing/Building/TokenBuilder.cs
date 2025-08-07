using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RCLargeLanguageModels.Parsing.Building.TokenPatterns;
using RCLargeLanguageModels.Parsing.TokenPatterns;

namespace RCLargeLanguageModels.Parsing.Building
{
	/// <summary>
	/// Represents a builder for constructing tokens for parsing.
	/// </summary>
	public class TokenBuilder
	{
		private BuildableTokenPattern? _pattern;

		/// <summary>
		/// Gets the token being built.
		/// </summary>
		public BuildableTokenPattern? BuildingPattern => _pattern;

		public TokenBuilder Add(BuildableTokenPattern childToken)
		{
			if (_pattern == null)
			{
				_pattern = childToken;
			}
			else if (_pattern is BuildableSequenceTokenPattern sequencePattern)
			{
				sequencePattern.Elements.Add(childToken);
			}
			else
			{
				var newSequence = new BuildableSequenceTokenPattern();
				newSequence.Elements.Add(_pattern);
				newSequence.Elements.Add(childToken);
				_pattern = newSequence;
			}
			return this;
		}

		public TokenBuilder Add(string tokenName)
		{
			if (_pattern is BuildableSequenceTokenPattern sequencePattern)
			{
				sequencePattern.Elements.Add(tokenName);
			}
			else
			{
				var newSequence = new BuildableSequenceTokenPattern();
				newSequence.Elements.Add(tokenName);
				_pattern = newSequence;
			}
			return this;
		}

		public TokenBuilder Token(string tokenName)
		{
			return Add(tokenName);
		}

		public TokenBuilder Add(TokenPattern token)
		{
			Add(new BuildableLeafTokenPattern { TokenPattern = token });
			return this;
		}

		public TokenBuilder SetParsedValueFactory(Func<List<ParsedToken>, object?>? parsedValueFactory)
		{
			if (_pattern is BuildableSequenceTokenPattern sequencePattern)
				sequencePattern.ParsedValueFactory = parsedValueFactory;
			else
				throw new ParserBuildingException("Parsed value factory can only be set on a sequence token pattern (must be added at least one named element or two child elements first).");
			return this;
		}

		public TokenBuilder Optional(Action<TokenBuilder> builderAction, Func<ParsedToken?, object?>? parsedValueFactory = null)
		{
			var builder = new TokenBuilder();
			builderAction(builder);

			if (builder.BuildingPattern == null)
				throw new ParserBuildingException("Optional child token pattern cannot be empty.");

			return Add(new BuildableOptionalTokenPattern
			{
				Child = builder.BuildingPattern,
				ParsedValueFactory = parsedValueFactory
			});
		}

		public TokenBuilder Repeat(int min, int max, Action<TokenBuilder> builderAction, Func<List<ParsedToken>, object?>? parsedValueFactory = null)
		{
			var builder = new TokenBuilder();
			builderAction(builder);

			if (builder.BuildingPattern == null)
				throw new ParserBuildingException("Repeated child token pattern cannot be empty.");

			return Add(new BuildableRepeatTokenPattern
			{
				MinCount = min,
				MaxCount = max,
				Child = builder.BuildingPattern,
				ParsedValueFactory = parsedValueFactory
			});
		}

		public TokenBuilder Repeat(int min, Action<TokenBuilder> builderAction, Func<List<ParsedToken>, object?>? parsedValueFactory = null)
		{
			return Repeat(min, -1, builderAction, parsedValueFactory);
		}

		public TokenBuilder ZeroOrMore(Action<TokenBuilder> builderAction, Func<List<ParsedToken>, object?>? parsedValueFactory = null)
		{
			return Repeat(0, -1, builderAction, parsedValueFactory);
		}

		public TokenBuilder OneOrMore(Action<TokenBuilder> builderAction, Func<List<ParsedToken>, object?>? parsedValueFactory = null)
		{
			return Repeat(1, -1, builderAction, parsedValueFactory);
		}

		public TokenBuilder Choice(Func<ParsedToken, object?>? parsedValueFactory, params Or<Action<TokenBuilder>, string>[] choices)
		{
			var builtValues = choices.Select(c =>
			{
				var builder = new TokenBuilder();

				if (c.VariantIndex == 0)
				{
					c.AsT1().Invoke(builder);
					if (builder.BuildingPattern == null)
						throw new ParserBuildingException("Choice child token pattern cannot be empty.");
					return new Or<string, BuildableTokenPattern>(builder.BuildingPattern);
				}
				else
				{
					var name = c.AsT2();
					return new Or<string, BuildableTokenPattern>(name);
				}

			}).ToList();

			var choice = new BuildableChoiceTokenPattern();
			choice.Choices.AddRange(builtValues);
			choice.ParsedValueFactory = parsedValueFactory;

			return Add(choice);
		}

		public TokenBuilder Choice(Func<ParsedToken, object?>? parsedValueFactory, params Action<TokenBuilder>[] choices)
		{
			return Choice(parsedValueFactory, choices.Select(c => new Or<Action<TokenBuilder>, string>(c)).ToArray());
		}

		public TokenBuilder Choice(params Or<Action<TokenBuilder>, string>[] choices)
		{
			return Choice(null, choices);
		}

		public TokenBuilder Choice(params Action<TokenBuilder>[] choices)
		{
			return Choice(null, choices);
		}

		public TokenBuilder Literal(string literal, Func<string, object?>? parsedValueFactory = null)
		{
			return Add(new LiteralTokenPattern(literal, parsedValueFactory));
		}

		public TokenBuilder Literal(string literal, StringComparer comparer, Func<string, object?>? parsedValueFactory = null)
		{
			return Add(new LiteralTokenPattern(literal, comparer, parsedValueFactory));
		}

		public TokenBuilder Regex(string regex, RegexOptions options = RegexOptions.Compiled, Func<Match, object?>? parsedValueFactory = null)
		{
			return Add(new RegexTokenPattern(regex, parsedValueFactory, options));
		}

		public TokenBuilder Regex(string regex, Func<Match, object?>? parsedValueFactory)
		{
			return Add(new RegexTokenPattern(regex, parsedValueFactory));
		}


	}
}