using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Locale
{
	// Это дипсик написал

	/// <summary>
	/// A simple language detector that uses character ranges to determine the language of a text.
	/// </summary>
	public class SimpleLanguageDetector : ILanguageDetector
	{
		// Dictionary of character ranges with their associated language families
		static Dictionary<string, (char start, char end)[]> scriptRanges = new Dictionary<string, (char start, char end)[]>
		{
			["Latin"] = new[] { ('A', 'Z'), ('a', 'z'), ('À', 'Ö'), ('Ø', 'ö'), ('ø', 'ƿ') },
			["Cyrillic"] = new[] { ('Ѐ', 'ӿ') },
			["Arabic"] = new[] { ('\u0600', '\u06FF'), ('\u0750', '\u077F'), ('\u08A0', '\u08FF') },
			["Devanagari"] = new[] { ('\u0900', '\u097F'), ('\uA8E0', '\uA8FF') },
			["Bengali"] = new[] { ('\u0980', '\u09FF') },
			["Han"] = new[] { ('\u4E00', '\u9FFF'), ('\u3400', '\u4DBF') }, // ('\u20000', '\u2A6DF') extension B
			["Hiragana"] = new[] { ('\u3040', '\u309F') },
			["Katakana"] = new[] { ('\u30A0', '\u30FF') },
			["Hangul"] = new[] { ('\uAC00', '\uD7AF'), ('\u1100', '\u11FF'), ('\u3130', '\u318F') },
			["Thai"] = new[] { ('\u0E00', '\u0E7F') },
			["Hebrew"] = new[] { ('\u0590', '\u05FF') },
			["Greek"] = new[] { ('\u0370', '\u03FF'), ('\u1F00', '\u1FFF') },
			["Georgian"] = new[] { ('\u10A0', '\u10FF') },
			["Armenian"] = new[] { ('\u0530', '\u058F') },
			["Tibetan"] = new[] { ('\u0F00', '\u0FFF') },
			["Ethiopic"] = new[] { ('\u1200', '\u137F') },
			["CanadianSyllabics"] = new[] { ('\u1400', '\u167F') },
			["Cherokee"] = new[] { ('\u13A0', '\u13FF') }
		};

		// Map scripts to their primary language codes
		static Dictionary<string, LanguageCode> scriptToLanguage = new Dictionary<string, LanguageCode>
		{
			["Latin"] = LanguageCode.English,
			["Cyrillic"] = LanguageCode.Russian,
			["Arabic"] = LanguageCode.Arabic,
			["Devanagari"] = LanguageCode.Hindi,
			["Bengali"] = LanguageCode.Bengali,
			["Han"] = LanguageCode.Chinese,
			["Hiragana"] = LanguageCode.Japanese,
			["Katakana"] = LanguageCode.Japanese,
			["Hangul"] = LanguageCode.Korean,
			["Thai"] = LanguageCode.Thai,
			["Hebrew"] = LanguageCode.Hebrew,
			["Greek"] = LanguageCode.Greek,
			["Georgian"] = LanguageCode.Georgian,
			["Armenian"] = LanguageCode.Armenian,
			["Tibetan"] = LanguageCode.Tibetan,
			["Ethiopic"] = LanguageCode.Amharic,
			["CanadianSyllabics"] = LanguageCode.Cherokee,
			["Cherokee"] = LanguageCode.Cherokee
		};

		/// <inheritdoc/>
		/// <exception cref="ArgumentNullException"/>
		public LanguageCode DetectLanguage(string text)
		{
			if (string.IsNullOrEmpty(text))
				throw new ArgumentNullException(nameof(text));

			// Count occurrences of each script's characters
			var scriptCounts = new Dictionary<string, int>();
			foreach (var script in scriptRanges.Keys)
			{
				scriptCounts[script] = 0;
			}

			foreach (char c in text)
			{
				foreach (var script in scriptRanges)
				{
					foreach (var range in script.Value)
					{
						if (c >= range.start && c <= range.end)
						{
							scriptCounts[script.Key]++;
							break; // No need to check other ranges for this script
						}
					}
				}
			}

			// Find the script with the highest count
			string detectedScript = scriptCounts.OrderByDescending(kvp => kvp.Value).First().Key;

			// If no characters matched any script ranges, return default (English)
			if (scriptCounts[detectedScript] == 0)
				return LanguageCode.English;

			// Special cases for Latin script languages
			if (detectedScript == "Latin")
			{
				// This is simplified - in a real implementation you'd use more sophisticated detection
				// for Latin-script languages. This just checks for some common words.

				string lowerText = text.ToLowerInvariant();

				// Check for Spanish
				if (lowerText.Contains(" el ") || lowerText.Contains(" los ") ||
					lowerText.Contains(" y ") || lowerText.Contains(" de ") ||
					lowerText.Contains(" que "))
					return LanguageCode.Spanish;

				// Check for French
				if (lowerText.Contains(" le ") || lowerText.Contains(" la ") ||
					lowerText.Contains(" et ") || lowerText.Contains(" de ") ||
					lowerText.Contains(" des "))
					return LanguageCode.French;

				// Check for German
				if (lowerText.Contains(" der ") || lowerText.Contains(" die ") ||
					lowerText.Contains(" das ") || lowerText.Contains(" und ") ||
					lowerText.Contains(" für "))
					return LanguageCode.German;

				// Default to English for Latin script
				return LanguageCode.English;
			}

			// Return the primary language for the detected script
			return scriptToLanguage[detectedScript];
		}
	}
}