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
		/// The current position in the input string.
		/// </summary>
		public int position;

		/// <summary>
		/// The current recursion depth.
		/// </summary>
		public int recursionDepth;

		/// <summary>
		/// The inherited settings that control the behavior of the parser during parsing operations.
		/// </summary>
		public ParserSettings settings;

		/// <summary>
		/// The parser object that is performing the parsing.
		/// </summary>
		public readonly Parser parser;

		/// <summary>
		/// A cache to store parsed results for reuse.
		/// </summary>
		public readonly ParserCache cache;

		/// <summary>
		/// A set of positions that have successfully parsed rules and tokens.
		/// </summary>
		/// <remarks>
		/// Used to retrive relevant errors that can be used for debugging purposes.
		/// </remarks>
		public readonly HashSet<int> successPositions;

		/// <summary>
		/// A list to store any parsing errors encountered during the process.
		/// </summary>
		public readonly List<ParsingError> errors;

		/// <summary>
		/// A list to store any rules that were skipped during the parsing process.
		/// </summary>
		public readonly List<ParsedRule> skippedRules;

		/// <summary>
		/// Gets a summary of first 10 parsing errors encountered during the process.
		/// </summary>
		/// <remarks>
		/// Needed for debugging purposes.
		/// </remarks>
		public string ErrorSummary => GetErrorSummary(10);

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
			recursionDepth = 0;
			settings = default;

			this.parser = parser ?? throw new ArgumentNullException(nameof(parser));
			settings = parser.Settings;

			this.cache = new ParserCache();
			this.successPositions = new HashSet<int>();
			this.errors = new List<ParsingError>();
			this.skippedRules = new List<ParsedRule>();
		}

		/// <summary>
		/// Creates a new instance of the <see cref="ParserContext"/> class.
		/// </summary>
		/// <param name="str">The input string to be parsed.</param>
		/// <param name="initialPosition">The initial position in the input string.</param>
		/// <param name="initialRecursionDepth">The initial recursion depth.</param>
		/// <param name="settings">The settings that control the behavior of the parser during parsing operations.</param>
		/// <param name="parser">The parser object that is performing the parsing.</param>
		/// <param name="cache">A cache to store parsed results for reuse.</param>
		/// <param name="successPositions">A set of positions that have successfully parsed rules and tokens.</param>
		/// <param name="errors">A list to store any parsing errors encountered during the process.</param>
		/// <param name="skippedRules">A list to store any rules that were skipped during the parsing process.</param>
		public ParserContext(string str, int initialPosition, int initialRecursionDepth, ParserSettings settings,
			Parser parser, ParserCache cache, HashSet<int> successPositions, List<ParsingError> errors, List<ParsedRule> skippedRules)
		{
			this.str = str ?? throw new ArgumentNullException(nameof(str));
			this.position = initialPosition;
			this.recursionDepth = initialRecursionDepth;
			this.settings = settings;

			this.parser = parser ?? throw new ArgumentNullException(nameof(parser));
			this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
			this.successPositions = successPositions ?? throw new ArgumentNullException(nameof(successPositions));
			this.errors = errors ?? throw new ArgumentNullException(nameof(errors));
			this.skippedRules = skippedRules ?? throw new ArgumentNullException(nameof(skippedRules));
		}

		/// <summary>
		/// Records, ignores or throws an error based on the current settings.
		/// </summary>
		/// <param name="error">The parsing error to record.</param>
		public void RecordError(ParsingError error)
		{
			switch (settings.errorHandling)
			{
				case ParserErrorHandlingMode.Default:
					errors.Add(error);
					break;

				case ParserErrorHandlingMode.NoRecord:
					break;

				case ParserErrorHandlingMode.Throw:
					throw error.ToException(this);
			}
		}

		/// <summary>
		/// Records, ignores or throws an error based on the current settings and using current position.
		/// </summary>
		/// <param name="errorMessage">The parsing error message to record.</param>
		public void RecordError(string errorMessage)
		{
			switch (settings.errorHandling)
			{
				case ParserErrorHandlingMode.Default:
					errors.Add(new ParsingError(position, errorMessage));
					break;

				case ParserErrorHandlingMode.NoRecord:
					break;

				case ParserErrorHandlingMode.Throw:
					throw new ParsingException(errorMessage, str, position);
			}
		}

		/// <summary>
		/// Returns a summary of all parsing errors encountered during the process, limited to a specified number of errors.
		/// </summary>
		/// <param name="maxErrors">The maximum number of errors to include in the summary.</param>
		/// <returns>A summary of the parsing errors.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the specified number of errors is negative.</exception>
		public string GetErrorSummary(int maxErrors)
		{
			if (maxErrors < 0)
				throw new ArgumentOutOfRangeException(nameof(maxErrors));
			if (errors.Count == 0)
				return "No errors encountered.";

			ParserContext t = this;

			return $"Errors ({errors.Count} total):\n" +
				$"{string.Join("\n\n", errors.Take(maxErrors).Select(e => e.ToString(t)))}" +
				$"{(errors.Count > maxErrors ? $"\n\nand {errors.Count - maxErrors} more..." : "")}";
		}

		/// <summary>
		/// Returns the most relevant parsing error encountered during the process.
		/// </summary>
		/// <remarks>
		/// Returns the last error with position that haven't parsed successfully.
		/// </remarks>
		/// <returns>The most relevant parsing error or <see langword="default"/>.</returns>
		public ParsingError GetMostRelevantError()
		{
			var successPos = successPositions;
			return errors.LastOrDefault(err => !successPos.Contains(err.position));
		}
	}
}