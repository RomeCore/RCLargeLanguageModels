using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RCLargeLanguageModels.Parsing
{
	/// <summary>
	/// Represents the context for parsing operations.
	/// </summary>
	public struct ParserContext
	{
		/// <summary>
		/// The input string to be parsed.
		/// </summary>
		public readonly string str;

		/// <summary>
		/// The current position in the input string during parsing.
		/// </summary>
		public int position;

		/// <summary>
		/// The parser object that is performing the parsing.
		/// </summary>
		public readonly Parser parser;

		/// <summary>
		/// A cache to store parsed results for reuse.
		/// </summary>
		public readonly ParserCache cache;

		/// <summary>
		/// A list to store any parsing errors encountered during the process.
		/// </summary>
		public readonly List<ParsingError> errors;

		/// <summary>
		/// Gets a summary of all parsing errors encountered during the process.
		/// </summary>
		/// <remarks>
		/// Needed for debugging purposes.
		/// </remarks>
		public string ErrorSummary
		{
			get
			{
				if (errors.Count == 0)
					return "No errors encountered.";

				ParserContext t = this;

				return $"Errors ({errors.Count} total):\n" +
					$"{string.Join("\n\n", errors.Take(10).Select(e => e.ToString(t)))}" +
					$"{(errors.Count > 10 ? $"\n\nand {errors.Count - 10} more..." : "")}";
			}
		}

		/// <summary>
		/// Gets the text after the current position in the input string.
		/// </summary>
		/// <remarks>
		/// Needed for debugging purposes.
		/// </remarks>
		public string TextAfterPosition
		{
			get
			{
				var substring = this.str.Substring(this.position);
				if (substring.Length > 20)
					return substring.Substring(0, 20) + "...";
				return substring;
			}
		}

		/// <summary>
		/// Creates a new instance of the <see cref="ParserContext"/> class.
		/// </summary>
		/// <param name="parser">The parser object that is performing the parsing.</param>
		/// <param name="str">The input string to be parsed.</param>
		public ParserContext(Parser parser, string str)
		{
			this.str = str ?? throw new ArgumentNullException(nameof(str));
			position = 0;

			this.parser = parser ?? throw new ArgumentNullException(nameof(parser));
			this.cache = new ParserCache();
			this.errors = new List<ParsingError>();
		}

		/// <summary>
		/// Creates a new instance of the <see cref="ParserContext"/> class.
		/// </summary>
		/// <param name="parser">The parser object that is performing the parsing.</param>
		/// <param name="str">The input string to be parsed.</param>
		/// <param name="cache">A cache to store parsed results for reuse.</param>
		/// <param name="initialPosition">The initial position in the input string.</param>
		/// <param name="errors">A list to store any parsing errors encountered during the process.</param>
		public ParserContext(Parser parser, string str, ParserCache cache, int initialPosition, List<ParsingError> errors)
		{
			this.str = str ?? throw new ArgumentNullException(nameof(str));
			position = initialPosition;

			this.parser = parser ?? throw new ArgumentNullException(nameof(parser));
			this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
			this.errors = errors ?? throw new ArgumentNullException(nameof(errors));
		}

		/// <summary>
		/// Skips characters in the input string that match the specified predicate.
		/// </summary>
		/// <param name="predicate">The predicate to match against.</param>
		public void Skip(Func<char, bool> predicate)
		{
			while (position < str.Length && predicate(str[position]))
				position++;
		}

		/// <summary>
		/// Skips whitespace characters in the input string.
		/// </summary>
		public void SkipWhiteSpace()
		{
			while (position < str.Length && char.IsWhiteSpace(str[position]))
				position++;
		}

		/// <summary>
		/// Creates a copy of the current parser context.
		/// </summary>
		/// <returns>A new instance of <see cref="ParserContext"/> with the same properties as the current one.</returns>
		public readonly ParserContext Copy()
		{
			return new ParserContext(parser, str, cache, position, errors);
		}

		/// <summary>
		/// Creates a copy of the current parser context with a new position.
		/// </summary>
		/// <returns>A new instance of <see cref="ParserContext"/> with the new position.</returns>
		public readonly ParserContext With(int newPosition)
		{
			return new ParserContext(parser, str, cache, newPosition, errors);
		}
	}
}