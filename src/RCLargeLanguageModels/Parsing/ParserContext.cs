using System;
using System.Collections.Generic;
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
		/// Creates a new instance of the <see cref="ParserContext"/> class.
		/// </summary>
		/// <param name="parser">The parser object that is performing the parsing.</param>
		/// <param name="str">The input string to be parsed.</param>
		public ParserContext(Parser parser, string str)
		{
			this.str = str ?? throw new ArgumentNullException(nameof(str));
			this.parser = parser ?? throw new ArgumentNullException(nameof(parser));
			this.cache = new ParserCache();
			position = 0;
		}

		/// <summary>
		/// Creates a new instance of the <see cref="ParserContext"/> class.
		/// </summary>
		/// <param name="parser">The parser object that is performing the parsing.</param>
		/// <param name="str">The input string to be parsed.</param>
		/// <param name="cache">A cache to store parsed results for reuse.</param>
		/// <param name="initialPosition">The initial position in the input string. Default is 0.</param>
		public ParserContext(Parser parser, string str, ParserCache cache, int initialPosition = 0)
		{
			this.str = str ?? throw new ArgumentNullException(nameof(str));
			this.parser = parser ?? throw new ArgumentNullException(nameof(parser));
			this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
			position = initialPosition;
		}

		/// <summary>
		/// Creates a copy of the current parser context.
		/// </summary>
		/// <returns>A new instance of <see cref="ParserContext"/> with the same input and current position.</returns>
		public readonly ParserContext Copy()
		{
			return new ParserContext(parser, str, cache, position);
		}

		/// <summary>
		/// Creates a copy of the current parser context with a new position.
		/// </summary>
		/// <returns>A new instance of <see cref="ParserContext"/> with the same input and new position.</returns>
		public readonly ParserContext With(int newPosition)
		{
			return new ParserContext(parser, str, cache, newPosition);
		}
	}
}