using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using RCLargeLanguageModels.Parsing.TokenPatterns;

namespace RCLargeLanguageModels.Parsing
{
	/// <summary>
	/// Represents a parsed token in the input text.
	/// </summary>
	public struct ParsedToken
	{
		/// <summary>
		/// The value indicates whether the parsing was successful.
		/// </summary>
		public readonly bool success;

		/// <summary>
		/// The ID of the token that was parsed.
		/// </summary>
		public int tokenId;

		/// <summary>
		/// The starting index of the token in the input text.
		/// </summary>
		public int startIndex;

		/// <summary>
		/// The length of the token in the input text.
		/// </summary>
		public int length;

		/// <summary>
		/// Gets the parsed value factory associated with this token.
		/// </summary>
		public Func<ParsedTokenResult, object?>? parsedValueFactory;

		/// <summary>
		/// Gets the intermediate value associated with this token.
		/// </summary>
		/// <remarks>
		/// For <see cref="SequenceTokenPattern"/> or <see cref="RepeatTokenPattern"/> it will be <see langword="null"/>. <br/>
		/// For <see cref="ChoiceTokenPattern"/> it will be the selected inner value. <br/>
		/// For <see cref="OptionalTokenPattern"/> it will be the inner value if present, otherwise null.
		/// <para/>
		/// For leaf token implementations this may be, for example,
		/// <see cref="Match"/> for <see cref="RegexTokenPattern"/>,
		/// or <see cref="char"/> for <see cref="CharRangeTokenPattern"/>. <br/>
		/// See remarks for specific implementations.
		/// </remarks>
		public object? intermediateValue;

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsedToken"/> struct.
		/// </summary>
		/// <param name="tokenId">The ID of the token that was parsed.</param>
		/// <param name="startIndex">The starting index of the token in the input text.</param>
		/// <param name="length">The length of the token.</param>
		/// <param name="parsedValueFactory">The parsed value factory associated with this token.</param>
		/// <param name="intermediateValue">The intermediate value associated with this token.</param>
		public ParsedToken(int tokenId, int startIndex, int length, Func<ParsedTokenResult, object?>? parsedValueFactory = null, object? intermediateValue = null)
		{
			this.success = true;
			this.tokenId = tokenId;
			this.startIndex = startIndex;
			this.length = length;
			this.parsedValueFactory = parsedValueFactory;
			this.intermediateValue = intermediateValue;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsedToken"/> struct.
		/// </summary>
		/// <param name="success">The value indicating whether the parsing was successful.</param>
		/// <param name="tokenId">The ID of the token that was parsed.</param>
		/// <param name="startIndex">The starting index of the token in the input text.</param>
		/// <param name="length">The length of the token.</param>
		/// <param name="parsedValueFactory">The parsed value factory associated with this token.</param>
		/// <param name="intermediateValue">The intermediate value associated with this token.</param>
		private ParsedToken(bool success, int tokenId, int startIndex, int length, Func<ParsedTokenResult, object?>? parsedValueFactory = null, object? intermediateValue = null)
		{
			this.success = success;
			this.tokenId = tokenId;
			this.startIndex = startIndex;
			this.length = length;
			this.parsedValueFactory = parsedValueFactory;
			this.intermediateValue = intermediateValue;
		}

		/// <summary>
		/// Gets a parsed token that represents failure.
		/// </summary>
		public static ParsedToken Fail { get; } = new ParsedToken(false, -1, -1, 0, null, null);
	}
}