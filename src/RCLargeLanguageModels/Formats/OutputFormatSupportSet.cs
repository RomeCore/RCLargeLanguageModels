using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Formats
{
	/// <summary>
	/// Represents a set of output formats. Used for <see cref="LLModel"/> descriptors.
	/// </summary>
	public class OutputFormatSupportSet : IEnumerable<OutputFormatType>
	{
		private readonly HashSet<OutputFormatType> _formats = new HashSet<OutputFormatType>();

		/// <summary>
		/// Gets the shared empty set of output formats.
		/// </summary>
		public static OutputFormatSupportSet Empty { get; } = new OutputFormatSupportSet(Enumerable.Empty<OutputFormatType>());

		/// <summary>
		/// Gets the shared marker set of output formats that should be considered as all-supporting.
		/// </summary>
		/// <remarks>
		/// Should be compared by references via <see cref="object.ReferenceEquals(object, object)"/>.
		/// </remarks>
		public static OutputFormatSupportSet All { get; } = new OutputFormatSupportSet(Enumerable.Empty<OutputFormatType>());

		/// <summary>
		/// Gets the shared set of text output formats (includes <see cref="OutputFormatType.Text"/> and <see cref="OutputFormatType.Markdown"/>).
		/// </summary>
		public static OutputFormatSupportSet Text { get; } = new OutputFormatSupportSet(OutputFormatType.Text, OutputFormatType.Markdown);

		/// <summary>
		/// Gets the shared set of text and json output formats (includes <see cref="OutputFormatType.Text"/>, <see cref="OutputFormatType.Markdown"/> and <see cref="OutputFormatType.Json"/>).
		/// </summary>
		public static OutputFormatSupportSet TextWithJson { get; } = new OutputFormatSupportSet(OutputFormatType.Text, OutputFormatType.Markdown, OutputFormatType.Json);

		/// <summary>
		/// Gets the shared set of text and json (with schema included) output formats (includes <see cref="OutputFormatType.Text"/>, <see cref="OutputFormatType.Markdown"/>, <see cref="OutputFormatType.Json"/> and <see cref="OutputFormatType.JsonSchema"/>).
		/// </summary>
		public static OutputFormatSupportSet TextWithJsonSchema { get; } = new OutputFormatSupportSet(OutputFormatType.Text, OutputFormatType.Markdown, OutputFormatType.Json, OutputFormatType.JsonSchema);

		/// <summary>
		/// Gets the count of output formats in the set.
		/// </summary>
		public int Count => _formats.Count;

		/// <summary>
		/// Creates a new instance of the <see cref="OutputFormatSupportSet"/> class.
		/// </summary>
		/// <param name="supportedFormats">The supported output formats.</param>
		/// <exception cref="ArgumentNullException">Thrown when the <paramref name="supportedFormats"/> is null.</exception>
		public OutputFormatSupportSet(IEnumerable<OutputFormatType> supportedFormats)
		{
			if (supportedFormats == null)
				throw new ArgumentNullException(nameof(supportedFormats));

			foreach (var format in supportedFormats)
			{
				_formats.Add(format);
			}
		}

		/// <summary>
		/// Creates a new instance of the <see cref="OutputFormatSupportSet"/> class.
		/// </summary>
		/// <param name="supportedFormats">The supported output formats.</param>
		/// <exception cref="ArgumentNullException">Thrown when the <paramref name="supportedFormats"/> is null.</exception>
		public OutputFormatSupportSet(params OutputFormatType[] supportedFormats)
			: this(supportedFormats as IEnumerable<OutputFormatType>)
		{
		}

		/// <summary>
		/// Checks if the specified output format is supported.
		/// </summary>
		/// <param name="format">The output format to check.</param>
		/// <returns><see langword="true"/> if the output format is supported; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the <paramref name="format"/> is null.</exception>
		public bool Supports(OutputFormatType format)
		{
			return _formats.Contains(format);
		}

		/// <summary>
		/// Checks if the specified output format is supported.
		/// </summary>
		/// <param name="formatId">The output format id to check.</param>
		/// <returns><see langword="true"/> if the output format is supported; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the <paramref name="formatId"/> is null.</exception>
		public bool Supports(string formatId)
		{
			var comparer = StringComparer.OrdinalIgnoreCase;
			return _formats.Any(f => comparer.Equals(f.Id, formatId));
		}

		/// <summary>
		/// Creates a copied <see cref="OutputFormatSupportSet"/> with the specified output format added.
		/// </summary>
		/// <param name="format">The output format to add.</param>
		/// <returns>A copied <see cref="OutputFormatSupportSet"/> with the specified output format added.</returns>
		public OutputFormatSupportSet With(OutputFormatType format)
		{
			return new OutputFormatSupportSet(_formats.Concat(new[] { format }));
		}

		/// <summary>
		/// Creates a copied <see cref="OutputFormatSupportSet"/> with the specified output formats added.
		/// </summary>
		/// <param name="formats">The output formats to add.</param>
		/// <returns>A copied <see cref="OutputFormatSupportSet"/> with the specified output formats added.</returns>
		public OutputFormatSupportSet With(IEnumerable<OutputFormatType> formats)
		{
			return new OutputFormatSupportSet(_formats.Concat(formats));
		}

		/// <summary>
		/// Creates a copied <see cref="OutputFormatSupportSet"/> with the specified output formats added.
		/// </summary>
		/// <param name="formats">The output formats to add.</param>
		/// <returns>A copied <see cref="OutputFormatSupportSet"/> with the specified output formats added.</returns>
		public OutputFormatSupportSet With(params OutputFormatType[] formats)
		{
			return new OutputFormatSupportSet(_formats.Concat(formats));
		}

		public override bool Equals(object obj)
		{
			if (obj is OutputFormatSupportSet other)
				return _formats.SetEquals(other._formats);
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return _formats.Aggregate(0, (acc, f) =>
			{
				unchecked
				{
					return (acc * 397) ^ f.GetHashCode();
				}
			});
		}

		public static bool operator ==(OutputFormatSupportSet left, OutputFormatSupportSet right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(OutputFormatSupportSet left, OutputFormatSupportSet right)
		{
			return !Equals(left, right);
		}

		public IEnumerator<OutputFormatType> GetEnumerator()
		{
			return _formats.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}