using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels
{
	/// <summary>
	/// Represents a pair of opening/closing characters for smart splitting
	/// </summary>
	public class CharPair
	{
		/// <summary>
		/// The opening character of the pair, may be equal to the closing character (e.g. '"')
		/// </summary>
		public char Opening { get; }

		/// <summary>
		/// The closing character of the pair, may be equal to the opening character (e.g. '"')
		/// </summary>
		public char Closing { get; }

		/// <summary>
		/// Indicates whether opening and closing characters are equal (e.g. '"')
		/// </summary>
		public bool CharsEqual { get; }

		public CharPair(char opening, char closing)
		{
			Opening = opening;
			Closing = closing;
			CharsEqual = Opening == Closing;
		}
	}

	public static class CharPairs
	{
		/// <summary>
		/// The pair of "
		/// </summary>
		public static CharPair Quotes { get; } = new CharPair('\"', '\"');

		/// <summary>
		/// The pair of '
		/// </summary>
		public static CharPair SingleQuotes { get; } = new CharPair('\'', '\'');

		/// <summary>
		/// The pair of ( and )
		/// </summary>
		public static CharPair Parentheses { get; } = new CharPair('(', ')');

		/// <summary>
		/// The pair of { and }
		/// </summary>
		public static CharPair Braces { get; } = new CharPair('{', '}');

		/// <summary>
		/// The pair of [ and ]
		/// </summary>
		public static CharPair Brackets { get; } = new CharPair('[', ']');

		/// <summary>
		/// The pair of &lt; and &gt;
		/// </summary>
		public static CharPair AngleBrackets { get; } = new CharPair('<', '>');

		/// <summary>
		/// List containing Quotes and SingleQuotes
		/// </summary>
		public static readonly IReadOnlyList<CharPair> PairsQuotes = new List<CharPair>
		{
			Quotes,
			SingleQuotes
		}.AsReadOnly();

		/// <summary>
		/// List containing Quotes, SingleQuotes, Parentheses and Braces
		/// </summary>
		public static readonly IReadOnlyList<CharPair> PairsDefaultSet = new List<CharPair>
		{
			Quotes,
			SingleQuotes,
			Parentheses,
			Braces
		}.AsReadOnly();

		/// <summary>
		/// List containing Quotes, SingleQuotes, Parentheses, Braces, Brackets and AngleBrackets
		/// </summary>
		public static readonly IReadOnlyList<CharPair> PairsFullSet = new List<CharPair>
		{
			Quotes,
			SingleQuotes,
			Parentheses,
			Braces,
			Brackets,
			AngleBrackets
		}.AsReadOnly();
	}
}