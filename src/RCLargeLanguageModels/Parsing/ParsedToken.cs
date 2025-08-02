using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Parsing
{
	/// <summary>
	/// Represents a parsed token in the input text.
	/// </summary>
	public readonly struct ParsedToken
	{
		/// <summary>
		/// The value indicates whether the parsing was successful.
		/// </summary>
		public readonly bool success;

		/// <summary>
		/// The ID of the token that was parsed.
		/// </summary>
		public readonly int tokenId;

		/// <summary>
		/// The starting index of the token in the input text.
		/// </summary>
		public readonly int startIndex;

		/// <summary>
		/// The length of the token in the input text.
		/// </summary>
		public readonly int length;

		/// <summary>
		/// Gets the parsed value associated with this token.
		/// </summary>
		public readonly object? parsedValue;

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsedToken"/> struct.
		/// </summary>
		/// <param name="tokenId">The ID of the token that was parsed.</param>
		/// <param name="startIndex">The starting index of the token in the input text.</param>
		/// <param name="length">The length of the token.</param>
		/// <param name="parsedValue">The parsed value associated with this token.</param>
		public ParsedToken(int tokenId, int startIndex, int length, object? parsedValue = null)
		{
			this.success = true;
			this.tokenId = tokenId;
			this.startIndex = startIndex;
			this.length = length;
			this.parsedValue = parsedValue;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsedToken"/> struct.
		/// </summary>
		/// <param name="success">The value indicating whether the parsing was successful.</param>
		/// <param name="tokenId">The ID of the token that was parsed.</param>
		/// <param name="startIndex">The starting index of the token in the input text.</param>
		/// <param name="length">The length of the token.</param>
		/// <param name="parsedValue">The parsed value associated with this token.</param>
		private ParsedToken(bool success, int tokenId, int startIndex, int length, object? parsedValue = null)
		{
			this.success = success;
			this.tokenId = tokenId;
			this.startIndex = startIndex;
			this.length = length;
			this.parsedValue = parsedValue;
		}

		/// <summary>
		/// Gets a parsed token that represents failure.
		/// </summary>
		public static ParsedToken Fail { get; } = new ParsedToken(false, -1, -1, 0, null);

		/// <summary>
		/// Gets a text contents of the parsed token.
		/// </summary>
		/// <param name="input">The input text.</param>
		/// <returns>The text representation of the parsed token.</returns>
		public string GetText(string input)
		{
			if (string.IsNullOrEmpty(input))
				throw new ArgumentException("Input text cannot be null or empty.", nameof(input));
			if (!success)
				throw new InvalidOperationException("Cannot get text for a failed token.");

			return input.Substring(startIndex, length);
		}

		/// <summary>
		/// Gets a text contents of the parsed token.
		/// </summary>
		/// <param name="context">The parser context that contains the input text.</param>
		/// <returns>The text representation of the parsed token.</returns>
		public string GetText(ParserContext context)
		{
			if (string.IsNullOrEmpty(context.str))
				throw new ArgumentException("Input text cannot be null or empty.", nameof(context));
			if (!success)
				throw new InvalidOperationException("Cannot get text for a failed token.");

			return context.str.Substring(startIndex, length);
		}

		/// <summary>
		/// Gets the value of the parsed token.
		/// </summary>
		/// <returns>The value of the parsed token.</returns>
		/// <remarks>
		/// Throws an exception if this result has no value or is a failure.
		/// </remarks>
		public object GetValue()
		{
			if (!success)
				throw new InvalidOperationException("Cannot get value for a failed rule.");
			if (parsedValue == null)
				throw new InvalidOperationException("Parsed value is not set.");
			return parsedValue;
		}

		/// <summary>
		/// Gets the value of the parsed token of the specified type.
		/// </summary>
		/// <returns>The value of the parsed token.</returns>
		/// <remarks>
		/// Throws an exception if this result has no value of type <typeparamref name="T"/> or is a failure.
		/// </remarks>
		public T GetValue<T>()
		{
			if (!success)
				throw new InvalidOperationException("Cannot get value for a failed rule.");
			if (parsedValue == null)
				throw new InvalidOperationException("Parsed value is not set.");
			if (parsedValue is T result)
				return result;
			throw new InvalidCastException($"The parsed value cannot be cast to {typeof(T)}, it is of type {parsedValue.GetType()}.");
		}
	}
}