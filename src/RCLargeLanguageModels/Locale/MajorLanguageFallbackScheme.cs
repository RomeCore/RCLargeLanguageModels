using System;
using System.Collections.Generic;
using System.Linq;

namespace RCLargeLanguageModels.Locale
{
	/// <summary>
	/// Provides a fallback scheme for languages based on major world languages.
	/// </summary>
	public class MajorLanguageFallbackScheme : ILanguageFallbackScheme
	{
		public LanguageCode GetFallbackLanguage(LanguageCode targetLanguage, IEnumerable<LanguageCode> availableLanguages)
		{
			if (availableLanguages == null)
				throw new ArgumentNullException(nameof(availableLanguages));
			if (!availableLanguages.Any())
				throw new ArgumentException("Available languages collection is empty.");

			if (availableLanguages.Any(l => l == targetLanguage))
				return targetLanguage;

			var hashSet = new HashSet<LanguageCode>(availableLanguages);
			hashSet.IntersectWith(LanguageGroup.MajorWorldLanguages);

			if (hashSet.Any())
				return hashSet.First();

			return availableLanguages.First();
		}
	}
}