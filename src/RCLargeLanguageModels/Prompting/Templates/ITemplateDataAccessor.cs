using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates
{
	/// <summary>
	/// Represents the data type of a template context data.
	/// </summary>
	public enum TemplateDataType
	{
		/// <summary>
		/// Represents a null value.
		/// </summary>
		Null,
		
		/// <summary>
		/// Represents a simple value, such as a string or an integer.
		/// </summary>
		Value,

		/// <summary>
		/// Represents a complex object.
		/// </summary>
		Object,

		/// <summary>
		/// Represents a collection of values.
		/// </summary>
		Array
	}

	/// <summary>
	/// Interface for accessing template data.
	/// </summary>
	public interface ITemplateDataAccessor : IDisposable
	{
		/// <summary>
		/// Gets the type of the data contained in the current context.
		/// </summary>
		TemplateDataType DataType { get; }

		/// <summary>
		/// Gets the length of the data if it is an array or a string.
		/// </summary>
		int Length { get; }

		/// <summary>
		/// Gets the keys of the data if it is an object.
		/// </summary>
		IEnumerable<string> Keys { get; }

		/// <summary>
		/// Gets the template data associated with the specified key.
		/// </summary>
		/// <param name="key">The key to retrieve the template data for.</param>
		/// <returns>The template data, or <see langword="null"/> if no data is found.</returns>
		ITemplateDataAccessor Get(string key);

		/// <summary>
		/// Gets the template data associated with the specified index.
		/// </summary>
		/// <param name="index">The index to retrieve the template data for.</param>
		/// <returns>The template data, or <see langword="null"/> if no data is found.</returns>
		ITemplateDataAccessor Index(int index);

		/// <summary>
		/// Gets the value of the current context as a boolean value.
		/// </summary>
		/// <returns>The value of the current context as a boolean value, or <see langword="false"/> if no data is found.</returns>
		bool AsBoolean();

		/// <summary>
		/// Converts the template data to a string representation.
		/// </summary>
		/// <param name="format">The format to use for converting the template data, or <see langword="null"/> to use the default format.</param>
		/// <returns>A string representing the template data.</returns>
		string ToString(string? format = null);
	}
}