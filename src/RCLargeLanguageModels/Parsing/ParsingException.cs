using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace RCLargeLanguageModels.Parsing
{
	/// <summary>
	/// Represents an exception that occurs during parsing.
	/// </summary>
	public class ParsingException : Exception
	{
		/// <summary>
		/// Gets the input where parsing failed.
		/// </summary>
		public string Input { get; }

		/// <summary>
		/// Gets the last error message during parsing.
		/// </summary>
		public string ErrorMessage { get; }

		/// <summary>
		/// Gets the list of original messages of the exception.
		/// </summary>
		public ImmutableList<string> ErrorMessages { get; }

		/// <summary>
		/// Gets the last position in the input where the error occurred.
		/// </summary>
		public int Position { get; }

		/// <summary>
		/// Gets the positions in the input where the errors occurred.
		/// </summary>
		public ImmutableList<int> Positions { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsingException"/> class.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="input">The input where parsing failed.</param>
		/// <param name="position">The position in the input where the error occurred.</param>
		public ParsingException(string message, string input, int position) : base(FormatMessage(message, input, position))
		{
			Input = input ?? throw new ArgumentNullException(nameof(input));
			ErrorMessage = message ?? throw new ArgumentNullException(nameof(message));
			ErrorMessages = ImmutableList.Create(message);
			Position = position;
			Positions = ImmutableList.Create(position);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsingException"/> class.
		/// </summary>
		/// <param name="input">The input where parsing failed.</param>
		/// <param name="errors">The list of parsing errors that occurred.</param>
		public ParsingException(string input, params ParsingError[] errors) : base(FormatMessage(input, errors))
		{
			if (errors == null)
				throw new ArgumentNullException(nameof(errors));
			if (!errors.Any())
				throw new ArgumentException("At least one error must be provided.", nameof(errors));

			Input = input;
			ErrorMessages = errors.Select(e => e.message).ToImmutableList();
			ErrorMessage = ErrorMessages[ErrorMessages.Count - 1];
			Positions = errors.Select(e => e.position).ToImmutableList();
			Position = Positions[Positions.Count - 1];
		}

		private static string FormatMessage(string message, string input, int position)
		{
			return $"{message}\n{PositionalFormatter.Format(input, position)}";
		}

		private static string FormatMessage(string input, params ParsingError[] errors)
		{
			if (errors.Length == 1)
				return FormatMessage(errors[0].message, input, errors[0].position);

			StringBuilder sb = new StringBuilder();
			const int max = 3;

			sb.AppendLine("Multiple errors occurred during parsing:");

			var groupedErrors = errors
				.GroupBy(e => e.position)
				.OrderByDescending(e => e.Key)
				.ToList();
			int last = Math.Min(groupedErrors.Count, max);

			for (int i = 0; i < last; i++)
			{
				var groupedError = groupedErrors[i];

				foreach (var error in groupedError.Distinct())
					sb.AppendLine(error.message);
				sb.Append(PositionalFormatter.Format(input, groupedError.Key));

				if (i < last - 1)
					sb.AppendLine().AppendLine();
			}

			if (groupedErrors.Count > max)
				sb.AppendLine().AppendLine().Append("...and more errors omitted.");

			return sb.ToString().Indent("  ", addIndentToFirstLine: false);
		}
	}
}