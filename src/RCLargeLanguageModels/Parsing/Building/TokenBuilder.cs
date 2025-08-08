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
		private Or<string, BuildableTokenPattern>? _pattern;

		/// <summary>
		/// Gets the token being built.
		/// </summary>
		public Or<string, BuildableTokenPattern>? BuildingPattern => _pattern;

		/// <summary>
		/// Gets the value indicating whether this builder has a valid pattern that can be built.
		/// </summary>
		public bool CanBeBuilt => _pattern.HasValue;

		/// <summary>
		/// Adds a token (name or child pattern) to the current pattern.
		/// </summary>
		/// <param name="childToken"></param>
		/// <returns></returns>
		public TokenBuilder Add(Or<string, BuildableTokenPattern> childToken)
		{
			if (!_pattern.HasValue)
			{
				_pattern = childToken;
			}
			else if (_pattern.Value.VariantIndex == 1 &&
					_pattern.Value.AsT2() is BuildableSequenceTokenPattern sequencePattern)
			{
				sequencePattern.Elements.Add(childToken);
			}
			else
			{
				var newSequence = new BuildableSequenceTokenPattern();
				newSequence.Elements.Add(_pattern.Value);
				newSequence.Elements.Add(childToken);
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
			if (_pattern?.AsT2() is BuildableSequenceTokenPattern sequencePattern)
				sequencePattern.ParsedValueFactory = parsedValueFactory;
			else
				throw new ParserBuildingException("Parsed value factory can only be set on a sequence token pattern (must be added at least two child elements first).");
			return this;
		}

		public TokenBuilder Optional(Action<TokenBuilder> builderAction, Func<ParsedToken?, object?>? parsedValueFactory = null)
		{
			var builder = new TokenBuilder();
			builderAction(builder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Optional child token pattern cannot be empty.");

			return Add(new BuildableOptionalTokenPattern
			{
				Child = builder.BuildingPattern.Value,
				ParsedValueFactory = parsedValueFactory
			});
		}

		public TokenBuilder Repeat(int min, int max, Action<TokenBuilder> builderAction, Func<List<ParsedToken>, object?>? parsedValueFactory = null)
		{
			var builder = new TokenBuilder();
			builderAction(builder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Repeated child token pattern cannot be empty.");

			return Add(new BuildableRepeatTokenPattern
			{
				MinCount = min,
				MaxCount = max,
				Child = builder.BuildingPattern.Value,
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
					if (!builder.CanBeBuilt)
						throw new ParserBuildingException("Choice child token pattern cannot be empty.");
					return builder.BuildingPattern.Value;
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

		public TokenBuilder EOF()
		{
			return Add(new EOFTokenPattern());
		}
	}
}