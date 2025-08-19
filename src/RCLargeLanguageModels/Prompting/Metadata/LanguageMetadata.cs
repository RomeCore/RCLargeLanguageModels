using System;
using System.Collections.Generic;
using System.Text;
using RCLargeLanguageModels.Locale;
using RCLargeLanguageModels.Metadata;

namespace RCLargeLanguageModels.Prompting.Metadata
{
	/// <summary>
	/// The metadata for language-related information.
	/// </summary>
	public class LanguageMetadata : IMetadata
	{
		/// <summary>
		/// Gets the language code associated with this metadata.
		/// </summary>
		public LanguageCode LanguageCode { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LanguageMetadata"/> class with the specified language code.
		/// </summary>
		/// <param name="languageCode">The language code associated with this metadata.</param>
		public LanguageMetadata(LanguageCode languageCode)
		{
			LanguageCode = languageCode;
		}

		public override bool Equals(object? obj)
		{
			return obj is LanguageMetadata other && LanguageCode == other.LanguageCode;
		}

		public override int GetHashCode()
		{
			int hash = 17;
			hash *= 397 + LanguageCode.GetHashCode();
			return hash;
		}
	}
}