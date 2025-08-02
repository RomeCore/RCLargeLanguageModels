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
		/// Initializes a new instance of the <see cref="ParsedToken"/> struct.
		/// </summary>
		/// <param name="tokenId">The ID of the token that was parsed.</param>
		/// <param name="startIndex">The starting index of the token in the input text.</param>
		/// <param name="length">The length of the token.</param>
		public ParsedToken(int tokenId, int startIndex, int length)
		{
			this.success = true;
			this.tokenId = tokenId;
			this.startIndex = startIndex;
			this.length = length;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsedToken"/> struct.
		/// </summary>
		/// <param name="success">The value indicating whether the parsing was successful.</param>
		/// <param name="tokenId">The ID of the token that was parsed.</param>
		/// <param name="startIndex">The starting index of the token in the input text.</param>
		/// <param name="length">The length of the token.</param>
		private ParsedToken(bool success, int tokenId, int startIndex, int length)
		{
			this.success = success;
			this.tokenId = tokenId;
			this.startIndex = startIndex;
			this.length = length;
		}

		/// <summary>
		/// Gets a parsed token that represents failure.
		/// </summary>
		public static ParsedToken Fail { get; } = new ParsedToken(false, -1, -1, 0);
	}
}