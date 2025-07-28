using System;

namespace RCLargeLanguageModels.Formats
{
	/// <summary>
	/// Represents a output format type.
	/// </summary>
	public readonly struct OutputFormatType
	{
		/// <summary>
		/// Gets the identifier of the output format.
		/// </summary>
		public string Id { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="OutputFormatType"/> class.
		/// </summary>
		/// <param name="id">The identifier of the output format.</param>
		/// <exception cref="ArgumentNullException">Thrown when the <paramref name="id"/> is null.</exception>
		public OutputFormatType(string id)
		{
			Id = id ?? throw new ArgumentNullException(nameof(id));
		}

		public override bool Equals(object obj)
		{
			if (obj is OutputFormatType other)
				return StringComparer.OrdinalIgnoreCase.Equals(Id, other.Id);
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return StringComparer.OrdinalIgnoreCase.GetHashCode(Id);
		}

		public static bool operator ==(OutputFormatType left, OutputFormatType right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(OutputFormatType left, OutputFormatType right)
		{
			return !Equals(left, right);
		}

		public override string ToString()
		{
			return Id;
		}

		/// <summary>
		/// The text output format type.
		/// </summary>
		public static OutputFormatType Text { get; } = new OutputFormatType("text");

		/// <summary>
		/// The markdown output format type.
		/// </summary>
		public static OutputFormatType Markdown { get; } = new OutputFormatType("md");

		/// <summary>
		/// The JSON output format type.
		/// </summary>
		public static OutputFormatType Json { get; } = new OutputFormatType("json");

		/// <summary>
		/// The output format type for JSON schema.
		/// </summary>
		public static OutputFormatType JsonSchema { get; } = new OutputFormatType("json/schema");

		/// <summary>
		/// The XML output format type.
		/// </summary>
		public static OutputFormatType Xml { get; } = new OutputFormatType("xml");

		/// <summary>
		/// The CSV output format type.
		/// </summary>
		public static OutputFormatType Csv { get; } = new OutputFormatType("csv");
	}
}