using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RCLargeLanguageModels.Locale;
using RCLargeLanguageModels.Metadata;

namespace RCLargeLanguageModels.Prompting.Metadata
{
	/// <summary>
	/// A fallback scheme for handling language metadata when no specific information is available.
	/// </summary>
	public class LanguageMetadataFallbackScheme : MetadataFallbackScheme<LanguageMetadata>
	{
		/// <summary>
		/// The language fallback scheme to use.
		/// </summary>
		public ILanguageFallbackScheme LanguageFallbackScheme { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LanguageMetadataFallbackScheme"/> class with the specified language fallback scheme.
		/// </summary>
		/// <param name="languageFallbackScheme">The language fallback scheme to use.</param>
		public LanguageMetadataFallbackScheme(ILanguageFallbackScheme languageFallbackScheme)
		{
			LanguageFallbackScheme = languageFallbackScheme;
		}

		protected override LanguageMetadata GetFallbackMetadataCore(LanguageMetadata targetMetadata, List<LanguageMetadata> availableMetadata)
		{
			return new LanguageMetadata(
				LanguageFallbackScheme.GetFallbackLanguage(
					targetMetadata.LanguageCode,
					availableMetadata.Select(m => m.LanguageCode)));
		}
	}
}