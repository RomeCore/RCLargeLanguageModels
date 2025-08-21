using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Locale
{
	/// <summary>
	/// Defines the fallback scheme for language codes.
	/// </summary>
	public interface ILanguageFallbackScheme
	{
		/// <summary>
		/// Gets the fallback language for a given target language and available languages.
		/// </summary>
		/// <param name="targetLanguage">The target language code.</param>
		/// <param name="availableLanguages">The available languages collection.</param>
		/// <returns>The fallback language code.</returns>
		LanguageCode GetFallbackLanguage(LanguageCode targetLanguage, IEnumerable<LanguageCode> availableLanguages);
	}
}