using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels
{
	/// <summary>
	/// Represents an immutable collection of stop sequences used in LLM completions.
	/// </summary>
	public class StopSequenceCollection : IEnumerable<string>
	{
		private readonly string[] _sequences;

		public string this[int index]
		{
			get { return _sequences[index]; }
		}

		/// <summary>
		/// Returns a count of stop sequences in collection.
		/// </summary>
		public int Count => _sequences.Length;

		/// <summary>
		/// Initializes a new empty instance of <see cref="StopSequenceCollection"/>.
		/// </summary>
		public StopSequenceCollection()
		{
			_sequences = Array.Empty<string>();
		}

		/// <summary>
		/// Initializes a new instance of <see cref="StopSequenceCollection"/> with one stop sequence.
		/// </summary>
		/// <param name="sequence">The stop sequence.</param>
		public StopSequenceCollection(string sequence)
		{
			if (string.IsNullOrEmpty(sequence))
				throw new ArgumentNullException(nameof(sequence));

			_sequences = new string[] { sequence };
		}
		
		/// <summary>
		/// Initializes a new instance of <see cref="StopSequenceCollection"/> with source collection of stop sequences.
		/// </summary>
		/// <param name="sequences">The source collection of stop sequences. Null elements will be ignored.</param>
		public StopSequenceCollection(IEnumerable<string> sequences)
		{
			if (sequences == null)
				throw new ArgumentNullException(nameof(sequences));

			_sequences = sequences.Where(s => !string.IsNullOrEmpty(s)).ToArray();
		}
		
		/// <summary>
		/// Initializes a new instance of <see cref="StopSequenceCollection"/> with source collection of stop sequences.
		/// </summary>
		/// <param name="sequences">The source collection of stop sequences. Null and empty elements will be ignored.</param>
		public StopSequenceCollection(params string[] sequences) : this(sequences as IEnumerable<string>)
		{
		}

		/// <summary>
		/// Validates stop sequence collection by checking count of sequences.
		/// If count of them is greater than <paramref name="maxCount"/>,
		/// the <see cref="InvalidOperationException"/> will be thrown.
		/// </summary>
		/// <param name="maxCount">The max count of stop sequences to consider this collection valid.</param>
		/// <exception cref="InvalidOperationException">Thrown if <see cref="Count"/> of them is greater than <paramref name="maxCount"/></exception>
		public void ValidateMaxCount(int maxCount)
		{
			if (_sequences.Length > maxCount)
				throw new InvalidOperationException($"Invalid count of stop sequences: {_sequences.Length} > max:{maxCount}");
		}

		/// <summary>
		/// Returns a string representation of the stop sequences, using ',' as separator and '@,' as escape sequence.
		/// </summary>
		public override string ToString()
		{
			if (_sequences.Length == 0)
				return string.Empty;

			var escapedSequences = _sequences.Select(s => s.Replace("@", "@@").Replace(",", "@,"));
			return string.Join(",", escapedSequences);
		}

		/// <summary>
		/// Parses a string representation of stop sequences into a StopSequenceCollection instance.
		/// </summary>
		/// <param name="value">The string to parse, using ',' as separator and '@,' as escape sequence.</param>
		/// <returns>A parsed <see cref="StopSequenceCollection"/> instance.</returns>
		public static StopSequenceCollection Parse(string value)
		{
			if (string.IsNullOrEmpty(value))
				return new StopSequenceCollection();

			var sequences = new List<string>();
			var currentSequence = new System.Text.StringBuilder();
			bool isEscaping = false;

			for (int i = 0; i < value.Length; i++)
			{
				char c = value[i];

				if (isEscaping)
				{
					if (c == ',' || c == '@')
					{
						currentSequence.Append(c);
					}
					else
					{
						// Invalid escape sequence, treat '@' as literal
						currentSequence.Append('@').Append(c);
					}
					isEscaping = false;
				}
				else
				{
					if (c == '@')
					{
						isEscaping = true;
					}
					else if (c == ',')
					{
						sequences.Add(currentSequence.ToString());
						currentSequence.Clear();
					}
					else
					{
						currentSequence.Append(c);
					}
				}
			}

			// Handle any remaining escape character at the end
			if (isEscaping)
			{
				currentSequence.Append('@');
			}

			// Add the last sequence
			if (currentSequence.Length > 0)
			{
				sequences.Add(currentSequence.ToString());
			}

			return new StopSequenceCollection(sequences);
		}

		public IEnumerator<string> GetEnumerator()
		{
			return ((IEnumerable<string>)_sequences).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_sequences).GetEnumerator();
		}

		public static implicit operator StopSequenceCollection(string sequence)
		{
			return new StopSequenceCollection(sequence);
		}

		public static implicit operator StopSequenceCollection(string[] sequences)
		{
			return new StopSequenceCollection(sequences);
		}
	}
}