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
	}
}