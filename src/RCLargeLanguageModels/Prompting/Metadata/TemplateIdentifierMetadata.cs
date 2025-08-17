using System;
using System.Collections.Generic;
using System.Text;
using RCLargeLanguageModels.Metadata;

namespace RCLargeLanguageModels.Prompting.Metadata
{
	/// <summary>
	/// The metadata for template identifier-related information.
	/// </summary>
	public class TemplateIdentifierMetadata : IMetadata
	{
		/// <summary>
		/// Gets the identifier for this metadata.
		/// </summary>
		public string Identifier { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateIdentifierMetadata"/> class with the specified identifier.
		/// </summary>
		/// <param name="identifier">The identifier for this metadata.</param>
		public TemplateIdentifierMetadata(string identifier)
		{
			Identifier = identifier;
		}
	}
}