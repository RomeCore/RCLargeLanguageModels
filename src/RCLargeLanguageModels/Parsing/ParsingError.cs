using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Parsing
{
	/// <summary>
	/// Represents a parsing error encountered during the parsing of input string.
	/// </summary>
	public readonly struct ParsingError
	{
		/// <summary>
		/// Gets the position in the input string where the error occurred.
		/// </summary>
		public readonly int position;

		/// <summary>
		/// Gets a description of the parsing error.
		/// </summary>
		public readonly string message;

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsingError"/> struct.
		/// </summary>
		/// <param name="position">The position in the input string where the error occurred.</param>
		/// <param name="message">A description of the parsing error.</param>
		public ParsingError(int position, string message)
		{
			this.position = position;
			this.message = message;
		}

		/// <summary>
		/// Returns a string that represents the parsing error with pretty formatted target line with line and column number informations.
		/// </summary>
		/// <param name="context">The parser context used for formatting.</param>
		/// <returns>A string that represents the parsing error.</returns>
		public string ToString(ParserContext context)
		{
			return $"{message}\n{PositionalFormatter.Format(context.str, position)}";
		}

		/// <summary>
		/// Converts the parsing error to a <see cref="ParsingException"/> with additional information from the provided <see cref="ParserContext"/>.
		/// </summary>
		/// <param name="context">The parser context to use for additional information.</param>
		/// <returns>An instance of <see cref="ParsingException"/> containing message, position and formatted input text.</returns>
		public ParsingException ToException(ParserContext context)
		{
			return new ParsingException(message, context.str, position);
		}
	}
}