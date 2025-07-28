using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCLargeLanguageModels.Locale;

namespace RCLargeLanguageModels.Locale
{
	/// <summary>
	/// An utility class that detects the language of the text.
	/// </summary>
	public static class LanguageDetector
	{
		private static ILanguageDetector _fallback = new SimpleLanguageDetector();

		/// <summary>
		/// The shared instance of the language detector. Can be <see langword="null"/> to use the simplified detector.
		/// </summary>
		public static ILanguageDetector Shared { get; set; }

		/// <summary>
		/// Detects the language of the text using the <see cref="Shared"/> instance.
		/// </summary>
		/// <param name="text">The text to detect the language of.</param>
		/// <returns>The language code of the text.</returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static LanguageCode DetectLanguage(string text)
		{
			if (string.IsNullOrEmpty(text))
				throw new ArgumentNullException(nameof(text));

			return Shared?.DetectLanguage(text) ?? _fallback.DetectLanguage(text);
		}
	}
}