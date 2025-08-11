using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace RCLargeLanguageModels.Parsing.TokenPatterns
{
	/// <summary>
	/// Matches as much text as possible that does not contain a specified set of forbidden sequences, 
	/// with support for escaping using a set of escape mappings. Escapes are processed and replaced 
	/// in the parsed value. Uses Tries for efficient lookup of escape and forbidden sequences.
	/// </summary>
	/// <remarks>
	/// Passes a captured <see cref="string"/> with replacements applied as an intermediate value.
	/// </remarks>
	public class EscapedTextTokenPattern : TokenPattern
	{
		private readonly TrieNode _escapeRoot;
		private readonly TrieNode _forbiddenRoot;

		/// <summary>
		/// The set of escape mappings to use for escaping sequences in the input text.
		/// </summary>
		public ImmutableDictionary<string, string> EscapeMappings { get; }

		/// <summary>
		/// The set of forbidden sequences that cannot appear in the input text if they are not escaped.
		/// </summary>
		public ImmutableHashSet<string> ForbiddenSequences { get; }

		/// <summary>
		/// The string comparer used for comparing and searching within the Trie nodes.
		/// </summary>
		public StringComparer Comparer { get; }

		/// <summary>
		/// The character comparer used for comparing and searching within the Trie nodes.
		/// </summary>
		public CharComparer CharComparer { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="EscapedTextTokenPattern"/> class.
		/// </summary>
		/// <param name="escapeMappings">The mappings for escape sequences to their replacements.</param>
		/// <param name="forbidden">The set of forbidden sequences that terminate the match if encountered unescaped.</param>
		/// <param name="comparer">The comparer to use for matching.</param>
		public EscapedTextTokenPattern(IEnumerable<KeyValuePair<string, string>> escapeMappings,
			IEnumerable<string> forbidden, StringComparer? comparer = null)
		{
			if (escapeMappings == null)
				throw new ArgumentNullException(nameof(escapeMappings));
			if (forbidden == null)
				throw new ArgumentNullException(nameof(forbidden));

			Comparer = comparer ?? StringComparer.Ordinal;
			CharComparer = new CharComparer(Comparer);
			EscapeMappings = ImmutableDictionary.CreateRange(Comparer, escapeMappings);
			ForbiddenSequences = ImmutableHashSet.CreateRange(Comparer, forbidden);
			_escapeRoot = new TrieNode(CharComparer);
			_forbiddenRoot = new TrieNode(CharComparer);

			foreach (var kvp in escapeMappings)
			{
				if (string.IsNullOrEmpty(kvp.Key))
					throw new ArgumentException("Escape sequences cannot be null or empty.", nameof(escapeMappings));
				AddToTrie(_escapeRoot, kvp.Key, kvp.Value);
			}

			foreach (var forb in forbidden)
			{
				if (string.IsNullOrEmpty(forb))
					throw new ArgumentException("Forbidden sequences cannot be null or empty.", nameof(forbidden));
				AddToTrie(_forbiddenRoot, forb, null);
			}
		}

		private void AddToTrie(TrieNode root, string sequence, string? replacement)
		{
			var node = root;
			foreach (var ch in sequence)
			{
				if (!node.Children.TryGetValue(ch, out var child))
				{
					child = new TrieNode(CharComparer);
					node.Children[ch] = child;
				}
				node = child;
			}
			node.IsTerminal = true;
			node.Replacement = replacement;
		}



		/// <summary>
		/// Creates a <see cref="EscapedTextTokenPattern"/> that matches the escaped text with double character escaping strategy.
		/// </summary>
		/// <remarks>
		/// For example, if the <paramref name="charSource"/> is "{}", then will be created a pattern
		/// with "{{" -> "{", "}}" -> "}" as escape sequences with "{" and "}" as forbidden sequences.
		/// </remarks>
		/// <param name="charSource">The source collection (or <see cref="string"/>) of characters to be escaped.</param>
		/// <param name="comparer">The comparer to use for matching.</param>
		/// <returns>A created <see cref="EscapedTextTokenPattern"/> instance.</returns>
		public static EscapedTextTokenPattern CreateDoubleCharacters(IEnumerable<char> charSource, StringComparer? comparer = null)
		{
			return new EscapedTextTokenPattern(
				charSource.Select(c => new KeyValuePair<string, string>(new string(c, 2), new string(c, 1))),
				charSource.Select(c => c.ToString()),
				comparer
			);
		}

		/// <summary>
		/// Creates a <see cref="EscapedTextTokenPattern"/> that matches the escaped text with double sequence escaping strategy.
		/// </summary>
		/// <remarks>
		/// For example, if the <paramref name="sequences"/> is ["ab", "bc"], then will be created a pattern
		/// with "abab" -> "ab", "bcbc" -> "bc" as escape sequences with "ab" and "bc" as forbidden sequences.
		/// </remarks>
		/// <param name="sequences">The source collection of sequences to be escaped.</param>
		/// <param name="comparer">The comparer to use for matching.</param>
		/// <returns>A created <see cref="EscapedTextTokenPattern"/> instance.</returns>
		public static EscapedTextTokenPattern CreateDoubleSequences(IEnumerable<string> sequences, StringComparer? comparer = null)
		{
			var sourceList = sequences.AsReadOnlyList();
			return new EscapedTextTokenPattern(
				sourceList.Select(c => new KeyValuePair<string, string>(c + c, c)),
				sourceList,
				comparer
			);
		}

		/// <summary>
		/// Creates a <see cref="EscapedTextTokenPattern"/> that matches the escaped text with prefix escaping strategy.
		/// </summary>
		/// <remarks>
		/// For example, if the <paramref name="charSource"/> is "abc" and <paramref name="prefix"/> is "\" (backslash),
		/// then will be created a pattern with "\a" -> "a", "\b" -> "b", "\c" -> "c" as escape sequences with "a", "b" and "c" as forbidden sequences.
		/// </remarks>
		/// <param name="charSource">The source collection (or <see cref="string"/>) of characters to be escaped.</param>
		/// <param name="prefix">The prefix used for escaping.</param>
		/// <param name="comparer">The comparer to use for matching.</param>
		/// <returns>A created <see cref="EscapedTextTokenPattern"/> instance.</returns>
		public static EscapedTextTokenPattern CreatePrefix(IEnumerable<char> charSource, string prefix = "\\", StringComparer? comparer = null)
		{
			return new EscapedTextTokenPattern(
				charSource.Select(c => new KeyValuePair<string, string>(prefix + c, c.ToString())),
				charSource.Select(c => c.ToString()),
				comparer
			);
		}

		/// <summary>
		/// Creates a <see cref="EscapedTextTokenPattern"/> that matches the escaped text with prefix escaping strategy.
		/// </summary>
		/// <remarks>
		/// For example, if the <paramref name="sequences"/> is ["ab", "bc"] and <paramref name="prefix"/> is "\" (backslash),
		/// then will be created a pattern with "\ab" -> "ab", "\bc" -> "bc" as escape sequences with "ab" and "bc" as forbidden sequences.
		/// </remarks>
		/// <param name="sequences">The source collection of sequences to be escaped.</param>
		/// <param name="prefix">The prefix used for escaping.</param>
		/// <param name="comparer">The comparer to use for matching.</param>
		/// <returns>A created <see cref="EscapedTextTokenPattern"/> instance.</returns>
		public static EscapedTextTokenPattern CreatePrefix(IEnumerable<string> sequences, string prefix = "\\", StringComparer? comparer = null)
		{
			var sourceList = sequences.AsReadOnlyList();
			return new EscapedTextTokenPattern(
				sourceList.Select(c => new KeyValuePair<string, string>(prefix + c, c)),
				sourceList,
				comparer
			);
		}



		public override bool TryMatch(ParserContext context, ParserContext childContext, out ParsedToken token)
		{
			int start = context.position;
			int pos = start;
			var input = context.str;
			var sb = new StringBuilder();

			while (pos < input.Length)
			{
				// 1) Try to match the longest escape starting at pos.
				//    If found — apply replacement and continue.
				if (TryMatchSequence(_escapeRoot, input, pos, out int escapeConsumed, out string? replacement))
				{
					sb.Append(replacement ?? string.Empty);
					pos += escapeConsumed;
					continue;
				}

				// 2) No escape terminal at this position.
				//    If a forbidden terminal starts here, stop (do not consume forbidden).
				if (TryMatchSequence(_forbiddenRoot, input, pos, out int forbiddenConsumed))
				{
					break; // unescaped forbidden sequence -> end of matched text
				}

				// 3) No terminal found for escape or forbidden.
				//    We must detect the *real* incomplete-escape case:
				//    If the remainder of the input starting at pos is a strict prefix of some escape
				//    AND we are at the end of the input (no more chars to try) => incomplete escape -> error.
				//    Otherwise treat the current char as normal text.
				if (IsStrictPrefixOfSomeEscape(input, pos))
				{
					// If the remaining input is a strict prefix of some escape and we are at EOF
					// (i.e. there are no more characters to complete that escape), then it's invalid.
					// We only error when pos..end matches prefix-of-some-escape and there are no more characters
					// that can arrive (we operate on the full string), so this is true incomplete escape.
					if (pos + (input.Length - pos) >= input.Length) // redundant but explicit: we are at end-of-input suffix
					{
						// Record error at the failure location
						context.RecordError("Invalid escape sequence.", pos);
						token = ParsedToken.Fail;
						return false;
					}
					// If we are not at EOF, we still append current char as normal — future iterations
					// may complete into a terminal escape (rare here because we scan the whole input).
				}

				// 4) Append normal character and advance.
				sb.Append(input[pos]);
				pos++;
			}

			// produce token; note: we allow zero-length result if you intentionally permit it.
			int length = pos - start;
			token = new ParsedToken(Id, start, length, ParsedValueFactory, sb.ToString());
			return true;
		}

		/// <summary>
		/// Try to match the longest terminal in the trie that starts at startPos.
		/// If found, returns true and sets consumed to the length and optional replacement.
		/// Otherwise returns false.
		/// </summary>
		private bool TryMatchSequence(TrieNode root, string input, int startPos, out int consumed, out string? replacement)
		{
			consumed = 0;
			replacement = null;

			int pos = startPos;
			var node = root;
			int lastTerminalPos = -1;
			string? lastReplacement = null;

			while (pos < input.Length)
			{
				char ch = input[pos];
				if (!node.Children.TryGetValue(ch, out var child))
					break;

				node = child;
				pos++;

				if (node.IsTerminal)
				{
					lastTerminalPos = pos;
					lastReplacement = node.Replacement;
				}
			}

			if (lastTerminalPos >= 0)
			{
				consumed = lastTerminalPos - startPos;
				replacement = lastReplacement;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Helper: returns true iff the substring input[startPos..] is a strict prefix of some escape sequence
		/// (i.e. you can walk the escape trie following all remaining chars, and at the final node there is
		/// at least one child and the node itself is not terminal).
		/// This indicates "we ended inside a branch of the trie" (possible incomplete escape).
		/// </summary>
		private bool IsStrictPrefixOfSomeEscape(string input, int startPos)
		{
			var node = _escapeRoot;
			int pos = startPos;

			// Walk as far as input goes.
			while (pos < input.Length)
			{
				char ch = input[pos];
				if (!node.Children.TryGetValue(ch, out var child))
					return false; // mismatch => not a prefix at all
				node = child;
				pos++;
			}

			// We consumed the whole remaining input along trie path.
			// If node is terminal => the remaining input itself *is* a terminal (should have been matched earlier).
			// If node is not terminal but has some children => it's a strict prefix of some longer escape.
			return !node.IsTerminal && node.Children.Count > 0;
		}

		private bool TryMatchSequence(TrieNode root, string input, int startPos, out int consumed)
		{
			string? dummyReplacement;
			return TryMatchSequence(root, input, startPos, out consumed, out dummyReplacement);
		}



		public override string ToString(int remainingDepth)
		{
			return $"escaped text with escapes: {string.Join(", ", EscapeMappings.Keys)} forbidden: {string.Join(", ", ForbiddenSequences)}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is EscapedTextTokenPattern other &&
				   EscapeMappings.SequenceEqual(other.EscapeMappings) &&
				   ForbiddenSequences.SetEquals(other.ForbiddenSequences) &&
				   Equals(Comparer, other.Comparer);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + EscapeMappings.GetSetHashCode(Comparer);
			hashCode = hashCode * -1521134295 + ForbiddenSequences.GetSetHashCode();
			hashCode = hashCode * -1521134295 + Comparer.GetHashCode();
			return hashCode;
		}

		private class TrieNode
		{
			public Dictionary<char, TrieNode> Children { get; }
			public bool IsTerminal { get; set; }
			public string? Replacement { get; set; }

			public TrieNode(CharComparer comparer)
			{
				Children = new Dictionary<char, TrieNode>(comparer);
			}
		}
	}
}