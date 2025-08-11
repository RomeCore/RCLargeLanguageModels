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
	public class TokenBuilder : ParserElementBuilder<TokenBuilder>
	{
		private Or<string, BuildableTokenPattern>? _pattern;

		/// <summary>
		/// Gets the token being built.
		/// </summary>
		public Or<string, BuildableTokenPattern>? BuildingPattern => _pattern;

		public override bool CanBeBuilt => _pattern.HasValue;
		protected override TokenBuilder GetThis() => this;
		public override TokenBuilder AddToken(Or<string, BuildableTokenPattern> childToken)
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

		/// <summary>
		/// Add a child pattern to the current sequence.
		/// </summary>
		/// <param name="tokenPattern">The child pattern to add.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="configurationAction">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public TokenBuilder AddToken(BuildableTokenPattern tokenPattern, Func<ParsedTokenResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? configurationAction = null)
		{
			tokenPattern.ParsedValueFactory = parsedValueFactory;
			configurationAction?.Invoke(tokenPattern.Settings);
			return AddToken(new Or<string, BuildableTokenPattern>(tokenPattern));
		}

		/// <summary>
		/// Converts the pattern into a sequence if it is not already one.
		/// </summary>
		/// <returns>Current instance for method chaining.</returns>
		public TokenBuilder ToSequence()
		{
			if (!_pattern.HasValue)
			{
				throw new ParserBuildingException("Cannot convert empty pattern to sequence.");
			}
			else if (_pattern.Value.VariantIndex != 1 ||
					_pattern.Value.AsT2() is not BuildableSequenceTokenPattern)
			{
				var newSequence = new BuildableSequenceTokenPattern();
				newSequence.Elements.Add(_pattern.Value);
				_pattern = newSequence;
			}
			return this;
		}

		/// <summary>
		/// Sets the transformation function to the current sequence pattern.
		/// </summary>
		/// <remarks>
		/// This method should be called after adding at least two child elements to the sequence.
		/// </remarks>
		/// <param name="parsedValueFactory">The transformation function (parsed value factory) to set.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the current pattern is not a sequence or has fewer than two child elements.</exception>
		public TokenBuilder Transform(Func<ParsedTokenResult, object?>? parsedValueFactory)
		{
			if (_pattern?.AsT2() is BuildableSequenceTokenPattern sequencePattern)
				sequencePattern.ParsedValueFactory = parsedValueFactory;
			else
				throw new ParserBuildingException("Parsed value factory can only be set on a sequence token pattern " +
					"(must be added at least two child elements or must be converted to a sequence first).");
			return this;
		}

		/// <summary>
		/// Configures the local settings for the current sequence pattern.
		/// </summary>
		/// <remarks>
		/// This method should be called after adding at least two child elements to the sequence.
		/// </remarks>
		/// <param name="configAction">The configuration action.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the current pattern is not a sequence or has fewer than two child elements.</exception>
		public TokenBuilder Configure(Action<ParserLocalSettingsBuilder> configAction)
		{
			if (_pattern?.AsT2() is BuildableSequenceTokenPattern sequenceRule)
				configAction(sequenceRule.Settings);
			else
				throw new ParserBuildingException("Only a sequence token pattern can be configured " +
					"(must be added at least two child elements or must be converted to a sequence first).");
			return this;
		}

		/// <summary>
		/// Adds an optional token pattern to the current sequence.
		/// </summary>
		/// <param name="builderAction">The action to build the optional token pattern.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="configurationAction">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder Optional(Action<TokenBuilder> builderAction, Func<ParsedTokenResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? configurationAction = null)
		{
			var builder = new TokenBuilder();
			builderAction(builder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Optional child token pattern cannot be empty.");

			return AddToken(new BuildableOptionalTokenPattern
			{
				Child = builder.BuildingPattern.Value
			}, parsedValueFactory, configurationAction);
		}

		/// <summary>
		/// Adds a repeatable token pattern to the current sequence with specified minimum and maximum occurrences.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the repeatable token.</param>
		/// <param name="min">The minimum number of times the token can be repeated.</param>
		/// <param name="max">The maximum number of times the token can be repeated. -1 indicates no upper limit.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="configurationAction">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder Repeat(Action<TokenBuilder> builderAction, int min, int max, Func<ParsedTokenResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? configurationAction = null)
		{
			var builder = new TokenBuilder();
			builderAction(builder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Repeated child token pattern cannot be empty.");

			return AddToken(new BuildableRepeatTokenPattern
			{
				MinCount = min,
				MaxCount = max,
				Child = builder.BuildingPattern.Value
			}, parsedValueFactory, configurationAction);
		}

		/// <summary>
		/// Adds a repeatable token pattern to the current sequence with specified minimum occurrences.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the repeatable token.</param>
		/// <param name="min">The minimum number of times the token can be repeated.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="configurationAction">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder Repeat(Action<TokenBuilder> builderAction, int min, Func<ParsedTokenResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? configurationAction = null)
		{
			return Repeat(builderAction, min, -1, parsedValueFactory, configurationAction);
		}

		/// <summary>
		/// Adds a repeatable token pattern to the current sequence that matches zero or more occurrences of the child pattern.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the repeatable token.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="configurationAction">The action to configure the local settings for this token.</param>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder ZeroOrMore(Action<TokenBuilder> builderAction, Func<ParsedTokenResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? configurationAction = null)
		{
			return Repeat(builderAction, 0, -1, parsedValueFactory, configurationAction);
		}

		/// <summary>
		/// Adds a repeatable token pattern to the current sequence that matches one or more occurrences of the child pattern.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the repeatable token.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="configurationAction">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder OneOrMore(Action<TokenBuilder> builderAction, Func<ParsedTokenResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? configurationAction = null)
		{
			return Repeat(builderAction, 1, -1, parsedValueFactory, configurationAction);
		}

		/// <summary>
		/// Adds a choice token pattern to the current sequence.
		/// </summary>
		/// <param name="choices">The choices for this token pattern.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="configurationAction">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public TokenBuilder Choice(IEnumerable<Or<Action<TokenBuilder>, string>> choices, Func<ParsedTokenResult, object?>? parsedValueFactory,
			Action<ParserLocalSettingsBuilder>? configurationAction = null)
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
			return AddToken(choice, parsedValueFactory, configurationAction);
		}

		/// <summary>
		/// Adds a choice token pattern to the current sequence.
		/// </summary>
		/// <param name="choices">The choices for this token pattern.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="configurationAction">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public TokenBuilder Choice(IEnumerable<Action<TokenBuilder>> choices, Func<ParsedTokenResult, object?>? parsedValueFactory,
			Action<ParserLocalSettingsBuilder>? configurationAction = null)
		{
			return Choice(choices.Select(c => new Or<Action<TokenBuilder>, string>(c)).ToArray(),
				parsedValueFactory, configurationAction);
		}

		/// <summary>
		/// Adds a choice token pattern to the current sequence.
		/// </summary>
		/// <param name="choices">The choices for this token pattern.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public TokenBuilder Choice(params Action<TokenBuilder>[] choices)
		{
			return Choice(choices, null, null);
		}
	}
}