using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using RCLargeLanguageModels.Parsing.Building.TokenPatterns;
using RCLargeLanguageModels.Parsing.TokenPatterns;

namespace RCLargeLanguageModels.Parsing.Building
{
	/// <summary>
	/// The base class for all parser element builders.
	/// </summary>
	public abstract class ParserElementBuilder<T>
	{
		/// <summary>
		/// Gets a value indicating whether the parser element can be built.
		/// </summary>
		public abstract bool CanBeBuilt { get; }

		/// <summary>
		/// Gets this instance. Used for fluent interface.
		/// </summary>
		protected abstract T GetThis();

		/// <summary>
		/// Adds a token (name or child pattern) to the current sequence.
		/// </summary>
		/// <param name="childToken">The token to add. Can be a name or a child pattern.</param>
		/// <returns>Current instance for method chaining.</returns>
		public abstract T AddToken(Or<string, BuildableTokenPattern> childToken);

		/// <summary>
		/// Adds a named token to the current sequence.
		/// </summary>
		/// <param name="tokenName">The name of the token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Token(string tokenName)
		{
			return AddToken(tokenName);
		}

		/// <summary>
		/// Add a child pattern to the current sequence.
		/// </summary>
		/// <param name="token">The child pattern to add.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="configurationAction">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T AddToken(TokenPattern token, Func<ParsedTokenResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? configurationAction = null)
		{
			var leafPattern = new BuildableLeafTokenPattern
			{
				TokenPattern = token,
				ParsedValueFactory = parsedValueFactory
			};
			configurationAction?.Invoke(leafPattern.Settings);
			AddToken(leafPattern);
			return GetThis();
		}

		/// <summary>
		/// Adds a character range token to the current sequence.
		/// </summary>
		/// <param name="minInclusive">Minimum inclusive character of the character range.</param>
		/// <param name="maxInclusive">Maximum inclusive character of the character range.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="configurationAction">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T CharRange(char minInclusive, char maxInclusive, Func<ParsedTokenResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? configurationAction = null)
		{
			return AddToken(new CharRangeTokenPattern(minInclusive, maxInclusive), parsedValueFactory, configurationAction);
		}

		/// <summary>
		/// Adds a literal token to the current sequence.
		/// </summary>
		/// <param name="literal">The literal string.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="configurationAction">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Literal(string literal, Func<ParsedTokenResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? configurationAction = null)
		{
			return AddToken(new LiteralTokenPattern(literal), parsedValueFactory, configurationAction);
		}

		/// <summary>
		/// Adds a literal token to the current sequence.
		/// </summary>
		/// <param name="literal">The literal string.</param>
		/// <param name="comparer">The string comparer to use.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="configurationAction">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Literal(string literal, StringComparer comparer, Func<ParsedTokenResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? configurationAction = null)
		{
			return AddToken(new LiteralTokenPattern(literal, comparer), parsedValueFactory, configurationAction);
		}

		/// <summary>
		/// Adds a literal choice token to the current sequence.
		/// </summary>
		/// <param name="literals">The literal strings set.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T LiteralChoice(params string[] literals)
		{
			return AddToken(new LiteralChoiceTokenPattern(literals));
		}

		/// <summary>
		/// Adds a literal choice token to the current sequence.
		/// </summary>
		/// <param name="literals">The literal strings set.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="configurationAction">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T LiteralChoice(IEnumerable<string> literals, Func<ParsedTokenResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? configurationAction = null)
		{
			return AddToken(new LiteralChoiceTokenPattern(literals), parsedValueFactory, configurationAction);
		}

		/// <summary>
		/// Adds a literal choice token to the current sequence.
		/// </summary>
		/// <param name="literals">The literal strings set.</param>
		/// <param name="comparer">The string comparer to use.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="configurationAction">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T LiteralChoice(IEnumerable<string> literals, StringComparer comparer, Func<ParsedTokenResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? configurationAction = null)
		{
			return AddToken(new LiteralChoiceTokenPattern(literals, comparer), parsedValueFactory, configurationAction);
		}

		/// <summary>
		/// Adds a regular expression token to the current sequence.
		/// </summary>
		/// <param name="regex">The regular expression.</param>
		/// <param name="options">The regular expression options. <see cref="RegexOptions.Compiled"/> by default.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="configurationAction">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Regex(string regex, RegexOptions options = RegexOptions.Compiled, Func<ParsedTokenResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? configurationAction = null)
		{
			return AddToken(new RegexTokenPattern(regex, options), parsedValueFactory, configurationAction);
		}

		/// <summary>
		/// Adds a regular expression token to the current sequence.
		/// </summary>
		/// <param name="regex">The regular expression.</param>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="configurationAction">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Regex(string regex, Func<ParsedTokenResult, object?>? parsedValueFactory,
			Action<ParserLocalSettingsBuilder>? configurationAction = null)
		{
			return AddToken(new RegexTokenPattern(regex), parsedValueFactory, configurationAction);
		}

		/// <summary>
		/// Adds a end of file (EOF) token to the current sequence.
		/// </summary>
		/// <param name="parsedValueFactory">The factory function to create a parsed value.</param>
		/// <param name="configurationAction">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EOF(Func<ParsedTokenResult, object?>? parsedValueFactory = null,
			Action<ParserLocalSettingsBuilder>? configurationAction = null)
		{
			return AddToken(new EOFTokenPattern(), parsedValueFactory, configurationAction);
		}
	}
}