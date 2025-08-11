using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace RCLargeLanguageModels.Parsing.TokenPatterns
{
	/// <summary>
	/// Matches one of a set of literal strings in the input text, using a Trie for efficient lookup.
	/// </summary>
	/// <remarks>
	/// Passes a matched original literal <see cref="string"/> (not captured) as an intermediate value.
	/// For example, if pattern was choice of "HELLO" or "WORLD" with case-insensitive comparison,
	/// then the intermediate value would be "HELLO" or "WORLD", not "hello" or "world".
	/// </remarks>
	public class LiteralChoiceTokenPattern : TokenPattern
	{
		/// <summary>
		/// The set of literal strings to match.
		/// </summary>
		private readonly TrieNode _root;

		/// <summary>
		/// Gets the set of literal strings to match.
		/// </summary>
		public ImmutableHashSet<string> Literals { get; }

		/// <summary>
		/// Gets the comparer used for matching.
		/// </summary>
		public StringComparer Comparer { get; }

		/// <summary>
		/// Gets the character comparer used for matching.
		/// </summary>
		public CharComparer CharComparer { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LiteralChoiceTokenPattern"/> class.
		/// </summary>
		/// <param name="literals">The collection of literal strings to match.</param>
		/// <param name="comparer">The comparer to use for matching.</param>
		public LiteralChoiceTokenPattern(IEnumerable<string> literals, StringComparer? comparer = null)
		{
			if (literals == null)
				throw new ArgumentNullException(nameof(literals));

			Comparer = comparer ?? StringComparer.Ordinal;
			CharComparer = new CharComparer(Comparer);
			Literals = ImmutableHashSet.CreateRange(Comparer, literals);
			_root = new TrieNode(CharComparer);

			foreach (var literal in literals)
			{
				if (string.IsNullOrEmpty(literal))
					throw new ArgumentException("Literals cannot contain null or empty strings.", nameof(literals));
				AddLiteral(literal);
			}
		}

		private void AddLiteral(string literal)
		{
			var node = _root;
			foreach (var ch in literal)
			{
				if (!node.Children.TryGetValue(ch, out var child))
				{
					child = new TrieNode(CharComparer);
					node.Children[ch] = child;
				}
				node = child;
			}
			node.IsTerminal = true;
			node.Literal = literal;
		}



		public override bool TryMatch(ParserContext context, ParserContext childContext, out ParsedToken token)
		{
			int pos = context.position;
			var node = _root;
			TrieNode? lastMatchNode = null;
			int lastMatchPos = pos;

			while (pos < context.str.Length)
			{
				char currentChar = context.str[pos];

				if (!node.Children.TryGetValue(currentChar, out var child))
					break;

				node = child;
				pos++;

				if (node.IsTerminal)
				{
					lastMatchNode = node;
					lastMatchPos = pos;
				}
			}

			if (lastMatchNode != null)
			{
				int length = lastMatchPos - context.position;
				token = new ParsedToken(Id, context.position, length, ParsedValueFactory, lastMatchNode.Literal);
				return true;
			}

			context.RecordError($"Failed to match {this}.");
			token = ParsedToken.Fail;
			return false;
		}



		public override string ToString(int remainingDepth)
		{
			return $"literal choice: '{string.Join("|", Literals)}'";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is LiteralChoiceTokenPattern other &&
				   Literals.SetEquals(other.Literals) &&
				   Equals(Comparer, other.Comparer);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + Literals.GetSetHashCode();
			hashCode = hashCode * -1521134295 + Comparer.GetHashCode();
			return hashCode;
		}

		private class TrieNode
		{
			public Dictionary<char, TrieNode> Children { get; }
			public bool IsTerminal { get; set; }
			public string? Literal { get; set; }

			public TrieNode(CharComparer comparer)
			{
				Children = new Dictionary<char, TrieNode>(comparer);
			}
		}
	}
}