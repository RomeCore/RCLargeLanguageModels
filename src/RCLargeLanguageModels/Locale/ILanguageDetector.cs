using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Locale
{
	/// <summary>
	/// Represents a language detecting interface.
	/// </summary>
	public interface ILanguageDetector
	{
		/// <summary>
		/// Detects the main language of the text and returns the language code.
		/// </summary>
		/// <param name="text">The text to detect language for.</param>
		/// <returns>The language code of detected language.</returns>
		LanguageCode DetectLanguage(string text);
	}
}