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
			if (position < 0 || position >= input.Length)
				return $"{message}\nPosition {position} is out of range (0..{input.Length - 1})";

			int lineNumber = 1;
			int column = 1;
			int currentLineStart = 0;
			bool prevWasCR = false;

			for (int i = 0; i <= position && i < input.Length; i++)
			{
				char c = input[i];

				if (c == '\r')
				{
					lineNumber++;
					column = 1;
					currentLineStart = i + 1;
					prevWasCR = true;
				}
				else if (c == '\n')
				{
					if (!prevWasCR)
					{
						lineNumber++;
						column = 1;
						currentLineStart = i + 1;
					}
					prevWasCR = false;
				}
				else
				{
					column++;
					prevWasCR = false;
				}
			}

			int lineEnd = input.Length;
			for (int i = position; i < input.Length; i++)
			{
				char c = input[i];
				if (c == '\r' || c == '\n')
				{
					lineEnd = i;
					break;
				}
			}

			string errorLine = input.Substring(currentLineStart, lineEnd - currentLineStart);

			int errorOffset = position - currentLineStart;
			string pointerLine = new string(' ', errorOffset) + '^';

			return $"{message}\n{errorLine}\n{pointerLine} line {lineNumber}, column {column}";
		}
	}
}