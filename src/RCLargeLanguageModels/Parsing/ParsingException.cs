using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Parsing
{
	/// <summary>
	/// Represents an exception that occurs during parsing.
	/// </summary>
	public class ParsingException : Exception
	{
		/// <summary>
		/// Gets the original message of the exception.
		/// </summary>
		public string OriginalMessage { get; }

		/// <summary>
		/// Gets the input where parsing failed.
		/// </summary>
		public string Input { get; }

		/// <summary>
		/// Gets the position in the input where the error occurred.
		/// </summary>
		public int Position { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsingException"/> class.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="input">The input where parsing failed.</param>
		/// <param name="position">The position in the input where the error occurred.</param>
		public ParsingException(string message, string input, int position) : base(FormatMessage(message, input, position))
		{
			OriginalMessage = message;
			Input = input;
			Position = position;
		}

		private static string FormatMessage(string message, string input, int position)
		{
			return $"{message}\n{PositionalFormatter.Format(input, position)}";
		}
	}
}