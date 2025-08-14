using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Parsing.TokenPatterns
{
	/// <summary>
	/// Token pattern that matches an identifier.
	/// </summary>
	public class IdentifierTokenPattern : TokenPattern
	{
		/// <summary>
		/// Predicate for the first character of an identifier.
		/// </summary>
		public Func<char, bool> StartPredicate { get; }

		/// <summary>
		/// Predicate for the remaining characters of an identifier.
		/// </summary>
		public Func<char, bool> ContinuePredicate { get; }

		/// <summary>
		/// Gets the minimum number of characters that the identifier must have.
		/// </summary>
		public int MinLength { get; }

		/// <summary>
		/// Gets the maximum number of characters that the identifier can have. -1 indicates no limit.
		/// </summary>
		public int MaxLength { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="IdentifierTokenPattern"/> class.
		/// </summary>
		/// <param name="startPredicate">The predicate for the first character of an identifier.</param>
		/// <param name="continuePredicate">The predicate for the remaining characters of an identifier.</param>
		/// <param name="minLength">The minimum number of characters that the identifier must have.</param>
		/// <param name="maxLength">The maximum number of characters that the identifier can have. -1 indicates no limit.</param>
		public IdentifierTokenPattern(Func<char, bool> startPredicate, Func<char, bool> continuePredicate, int minLength = 1, int maxLength = -1)
		{
			if (minLength < 0)
				throw new ArgumentOutOfRangeException(nameof(minLength), "minLength must be greater than or equal to 0");

			if (maxLength < minLength && maxLength != -1)
				throw new ArgumentOutOfRangeException(nameof(maxLength), "maxLength must be greater than or equal to minLength or be -1 if no maximum is specified.");

			MinLength = minLength;
			MaxLength = maxLength;

			StartPredicate = startPredicate ?? throw new ArgumentNullException(nameof(startPredicate));
			ContinuePredicate = continuePredicate ?? throw new ArgumentNullException(nameof(continuePredicate));
		}

		/// <summary>
		/// Creates a new instance of the <see cref="IdentifierTokenPattern"/> class that matches ASCII identifiers.
		/// </summary>
		public static IdentifierTokenPattern AsciiIdentifier(int minLength = 1, int maxLength = -1)
		{
			return new IdentifierTokenPattern(
				c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_',
				c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_',
				minLength, maxLength);
		}

		/// <summary>
		/// Creates a new instance of the <see cref="IdentifierTokenPattern"/> class that matches Unicode identifiers.
		/// </summary>
		public static IdentifierTokenPattern UnicodeIdentifier(int minLength = 1, int maxLength = -1)
		{
			return new IdentifierTokenPattern(
				c => char.IsLetter(c) || c == '_',
				c => char.IsLetterOrDigit(c) || c == '_',
				minLength, maxLength);
		}

		public override bool TryMatch(ParserContext context, ParserContext childContext, out ParsedToken token)
		{
			int startPos = context.position;
			int length = 0;
			string s = context.str;

			if (startPos >= s.Length)
			{
				token = ParsedToken.Fail;
				return false;
			}

			char c0 = s[startPos];
			if (!StartPredicate(c0))
			{
				token = ParsedToken.Fail;
				return false;
			}

			length = 1;
			int pos = startPos + 1;

			while (pos < s.Length &&
				   (MaxLength == -1 || length < MaxLength) &&
				   ContinuePredicate(s[pos]))
			{
				length++;
				pos++;
			}

			if (length < MinLength)
			{
				token = ParsedToken.Fail;
				return false;
			}

			context.position = startPos + length;
			token = new ParsedToken(Id, startPos, length);
			return true;
		}

		public override string ToString(int remainingDepth)
		{
			string range = MaxLength == -1 ? $"{MinLength}.." : $"{MinLength}..{MaxLength}";
			return $"identifier{{{range}}}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is IdentifierTokenPattern other &&
				   MinLength == other.MinLength &&
				   MaxLength == other.MaxLength &&
				   StartPredicate == other.StartPredicate &&
				   ContinuePredicate == other.ContinuePredicate;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = base.GetHashCode();
				hash = (hash * 397) ^ MinLength;
				hash = (hash * 397) ^ MaxLength;
				hash = (hash * 397) ^ StartPredicate.GetHashCode();
				hash = (hash * 397) ^ ContinuePredicate.GetHashCode();
				return hash;
			}
		}
	}
}