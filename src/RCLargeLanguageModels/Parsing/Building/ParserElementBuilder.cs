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
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T AddToken(TokenPattern token, Func<ParsedTokenResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			var leafPattern = new BuildableLeafTokenPattern
			{
				TokenPattern = token,
				ParsedValueFactory = factory
			};
			config?.Invoke(leafPattern.Settings);
			AddToken(leafPattern);
			return GetThis();
		}



		/// <summary>
		/// Adds a literal token to the current sequence.
		/// </summary>
		/// <param name="literal">The literal character.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Literal(char literal, Func<ParsedTokenResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new LiteralCharTokenPattern(literal), factory, config);
		}

		/// <summary>
		/// Adds a literal token to the current sequence.
		/// </summary>
		/// <param name="literal">The literal character.</param>
		/// <param name="comparison">The string comparison to use.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Literal(char literal, StringComparison comparison, Func<ParsedTokenResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new LiteralCharTokenPattern(literal, comparison), factory, config);
		}

		/// <summary>
		/// Adds a literal token to the current sequence.
		/// </summary>
		/// <param name="literal">The literal string.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Literal(string literal, Func<ParsedTokenResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			if (literal.Length == 1)
				return Literal(literal[0], factory, config);
			return AddToken(new LiteralTokenPattern(literal), factory, config);
		}

		/// <summary>
		/// Adds a literal token to the current sequence.
		/// </summary>
		/// <param name="literal">The literal string.</param>
		/// <param name="comparison">The string comparison to use.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Literal(string literal, StringComparison comparison, Func<ParsedTokenResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			if (literal.Length == 1)
				return Literal(literal[0], comparison, factory, config);
			return AddToken(new LiteralTokenPattern(literal, comparison), factory, config);
		}

		/// <summary>
		/// Adds a character predicate token to the current sequence.
		/// </summary>
		/// <param name="charPredicate">The character predicate to use.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Char(Func<char, bool> charPredicate, Func<ParsedTokenResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new CharacterTokenPattern(charPredicate), factory, config);
		}

		/// <summary>
		/// Adds a repeatable character predicate token to the current sequence with specified minimum occurrences.
		/// </summary>
		/// <param name="charPredicate">The character predicate to use.</param>
		/// <param name="minCount">The minimum inclusive number of characters to match.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Chars(Func<char, bool> charPredicate, int minCount, Func<ParsedTokenResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new RepeatCharactersTokenPattern(charPredicate, minCount, -1),
				factory, config);
		}

		/// <summary>
		/// Adds a repeatable character predicate token to the current sequence with specified minimum and maximum occurrences.
		/// </summary>
		/// <param name="charPredicate">The character predicate to use.</param>
		/// <param name="minCount">The minimum inclusive number of characters to match.</param>
		/// <param name="maxCount">The maximum inclusive number of characters to match. -1 means no limit.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Chars(Func<char, bool> charPredicate, int minCount, int maxCount, Func<ParsedTokenResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new RepeatCharactersTokenPattern(charPredicate, minCount, maxCount),
				factory, config);
		}

		/// <summary>
		/// Adds a repeatable character predicate token to the current sequence that matches zero or more occurrences.
		/// </summary>
		/// <param name="charPredicate">The character predicate to use.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T ZeroOrMoreChars(Func<char, bool> charPredicate, Func<ParsedTokenResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new RepeatCharactersTokenPattern(charPredicate, 0, -1),
				factory, config);
		}

		/// <summary>
		/// Adds a repeatable character predicate token to the current sequence that matches one or more occurrences.
		/// </summary>
		/// <param name="charPredicate">The character predicate to use.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T OneOrMoreChars(Func<char, bool> charPredicate, Func<ParsedTokenResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new RepeatCharactersTokenPattern(charPredicate, 1, -1),
				factory, config);
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
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T LiteralChoice(IEnumerable<string> literals, Func<ParsedTokenResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new LiteralChoiceTokenPattern(literals), factory, config);
		}

		/// <summary>
		/// Adds a literal choice token to the current sequence.
		/// </summary>
		/// <param name="literals">The literal strings set.</param>
		/// <param name="comparer">The string comparer to use.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T LiteralChoice(IEnumerable<string> literals, StringComparer comparer, Func<ParsedTokenResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new LiteralChoiceTokenPattern(literals, comparer), factory, config);
		}

		/// <summary>
		/// Adds a regular expression token to the current sequence.
		/// </summary>
		/// <param name="regex">The regular expression.</param>
		/// <param name="options">The regular expression options. <see cref="RegexOptions.Compiled"/> by default.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Regex(string regex, RegexOptions options = RegexOptions.Compiled, Func<ParsedTokenResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new RegexTokenPattern(regex, options), factory, config);
		}

		/// <summary>
		/// Adds a regular expression token to the current sequence.
		/// </summary>
		/// <param name="regex">The regular expression.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Regex(string regex, Func<ParsedTokenResult, object?>? factory,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new RegexTokenPattern(regex), factory, config);
		}

		/// <summary>
		/// Adds a whitespace token to the current sequence.
		/// </summary>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Whitespaces(Func<ParsedTokenResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new WhitespacesTokenPattern(), factory, config);
		}

		/// <summary>
		/// Adds a end of file (EOF) token to the current sequence.
		/// </summary>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EOF(Func<ParsedTokenResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new EOFTokenPattern(), factory, config);
		}

		#region EscapedText

		/// <summary>
		/// Adds an escaped text token to the current sequence with custom escape mappings and forbidden sequences.
		/// </summary>
		/// <param name="escapeMappings">The mappings for escape sequences to their replacements.</param>
		/// <param name="forbidden">The set of forbidden sequences that terminate the match.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedText(IEnumerable<KeyValuePair<string, string>> escapeMappings, IEnumerable<string> forbidden,
			Func<ParsedTokenResult, object?>? factory = null, Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new EscapedTextTokenPattern(escapeMappings, forbidden), factory, config);
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with custom escape mappings, forbidden sequences, and string comparer.
		/// </summary>
		/// <param name="escapeMappings">The mappings for escape sequences to their replacements.</param>
		/// <param name="forbidden">The set of forbidden sequences that terminate the match.</param>
		/// <param name="comparer">The string comparer to use.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedText(IEnumerable<KeyValuePair<string, string>> escapeMappings, IEnumerable<string> forbidden, StringComparer? comparer,
			Func<ParsedTokenResult, object?>? factory = null, Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new EscapedTextTokenPattern(escapeMappings, forbidden, comparer), factory, config);
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with double character escaping strategy.
		/// </summary>
		/// <param name="charSource">The source string of characters to be escaped.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextDoubleCharacters(IEnumerable<char> charSource,
			Func<ParsedTokenResult, object?>? factory = null, Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(EscapedTextTokenPattern.CreateDoubleCharacters(charSource), factory, config);
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with double character escaping strategy.
		/// </summary>
		/// <param name="charSource">The source string of characters to be escaped.</param>
		/// <param name="comparer">The string comparer to use.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextDoubleCharacters(IEnumerable<char> charSource, StringComparer comparer,
			Func<ParsedTokenResult, object?>? factory = null, Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(EscapedTextTokenPattern.CreateDoubleCharacters(charSource, comparer), factory, config);
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with double sequence escaping strategy.
		/// </summary>
		/// <param name="sequences">The source collection of sequences to be escaped.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextDoubleSequences(IEnumerable<string> sequences,
			Func<ParsedTokenResult, object?>? factory = null, Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(EscapedTextTokenPattern.CreateDoubleSequences(sequences), factory, config);
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with double sequence escaping strategy.
		/// </summary>
		/// <param name="sequences">The source collection of sequences to be escaped.</param>
		/// <param name="comparer">The string comparer to use.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextDoubleSequences(IEnumerable<string> sequences, StringComparer? comparer,
			Func<ParsedTokenResult, object?>? factory = null, Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(EscapedTextTokenPattern.CreateDoubleSequences(sequences, comparer), factory, config);
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with double sequence escaping strategy.
		/// </summary>
		/// <param name="sequences">The source collection of sequences to be escaped.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextDoubleSequences(params string[] sequences)
		{
			return AddToken(EscapedTextTokenPattern.CreateDoubleSequences(sequences), null, null);
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with prefix escaping strategy.
		/// </summary>
		/// <param name="charSource">The source collection (or <see cref="string"/>) of characters to be escaped.</param>
		/// <param name="prefix">The prefix used for escaping.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextPrefix(IEnumerable<char> charSource, string prefix = "\\",
			Func<ParsedTokenResult, object?>? factory = null, Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(EscapedTextTokenPattern.CreatePrefix(charSource, prefix), factory, config);
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with prefix escaping strategy.
		/// </summary>
		/// <param name="charSource">The source collection (or <see cref="string"/>) of characters to be escaped.</param>
		/// <param name="prefix">The prefix used for escaping.</param>
		/// <param name="comparer">The string comparer to use.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextPrefix(IEnumerable<char> charSource, string prefix, StringComparer comparer,
			Func<ParsedTokenResult, object?>? factory = null, Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(EscapedTextTokenPattern.CreatePrefix(charSource, prefix, comparer), factory, config);
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with prefix sequence escaping strategy.
		/// </summary>
		/// <param name="sequences">The source collection of sequences to be escaped.</param>
		/// <param name="prefix">The prefix used for escaping.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextPrefix(IEnumerable<string> sequences, string prefix = "\\",
			Func<ParsedTokenResult, object?>? factory = null, Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(EscapedTextTokenPattern.CreatePrefix(sequences, prefix), factory, config);
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with prefix sequence escaping strategy.
		/// </summary>
		/// <param name="sequences">The source collection of sequences to be escaped.</param>
		/// <param name="prefix">The prefix used for escaping.</param>
		/// <param name="comparer">The string comparer to use.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextPrefix(IEnumerable<string> sequences, string prefix, StringComparer comparer,
			Func<ParsedTokenResult, object?>? factory = null, Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(EscapedTextTokenPattern.CreatePrefix(sequences, prefix, comparer), factory, config);
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with prefix sequence escaping strategy.
		/// </summary>
		/// <param name="prefix">The prefix used for escaping.</param>
		/// <param name="sequences">The source collection of sequences to be escaped.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextPrefix(string prefix, params string[] sequences)
		{
			return AddToken(EscapedTextTokenPattern.CreatePrefix(sequences, prefix), null, null);
		}

		#endregion
	}
}