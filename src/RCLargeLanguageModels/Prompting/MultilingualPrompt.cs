using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCLargeLanguageModels.Locale;

namespace RCLargeLanguageModels.Prompting
{
	/// <summary>
	/// Represents a multilingual prompt that can be used with different languages.
	/// </summary>
	public class MultilingualPrompt
	{
		private readonly Dictionary<LanguageCode, string> _localizedPrompts = new Dictionary<LanguageCode, string>();
		private static readonly ILanguageFallbackScheme _fallbackScheme = new MajorLanguageFallbackScheme();

		/// <summary>
		/// Initializes a new empty instance of the <see cref="MultilingualPrompt"/> class.
		/// </summary>
		public MultilingualPrompt()
		{
		}

		/// <summary>
		/// Tries to add a prompt for a specific language.
		/// </summary>
		/// <param name="language">The language code that prompt is for.</param>
		/// <param name="prompt">The prompt text.</param>
		/// <returns><see langword="true"/> if the prompt was added; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="prompt"/> is <see langword="null"/>.</exception>
		public bool Add(LanguageCode language, string prompt)
		{
			if (string.IsNullOrEmpty(prompt))
				throw new ArgumentNullException(nameof(prompt));

			if (_localizedPrompts.ContainsKey(language))
				return false;

			_localizedPrompts[language] = prompt;
			return true;
		}

		/// <summary>
		/// Sets a prompt for a specific language.
		/// </summary>
		/// <param name="language">The language code that prompt is for.</param>
		/// <param name="prompt">The prompt text.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="prompt"/> is <see langword="null"/>.</exception>
		public void Set(LanguageCode language, string prompt)
		{
			if (string.IsNullOrEmpty(prompt))
				throw new ArgumentNullException(nameof(prompt));

			_localizedPrompts[language] = prompt;
		}

		/// <summary>
		/// Removes a prompt for a specific language.
		/// </summary>
		/// <param name="language">The language code that prompt is will be removed.</param>
		/// <returns><see langword="true"/> if the prompt was removed; otherwise, <see langword="false"/>.</returns>
		public bool Remove(LanguageCode language)
		{
			return _localizedPrompts.Remove(language);
		}

		/// <summary>
		/// Gets the prompt for a specific language using default fallback scheme.
		/// </summary>
		/// <param name="language">The language code.</param>
		/// <returns>The prompt for the specified or fallback language.</returns>
		public string GetPrompt(LanguageCode language)
		{
			return GetPrompt(language, _fallbackScheme);
		}

		/// <summary>
		/// Gets the prompt for a specific language using specific fallback scheme.
		/// </summary>
		/// <param name="language">The language code.</param>
		/// <param name="fallbackScheme">The fallback scheme to use when there is not prompt for that language.</param>
		/// <returns>The prompt for the specified or fallback language.</returns>
		public string GetPrompt(LanguageCode language, ILanguageFallbackScheme fallbackScheme)
		{
			if (fallbackScheme == null)
				throw new ArgumentNullException(nameof(fallbackScheme));
			if (_localizedPrompts.Count == 0)
				throw new InvalidOperationException("No prompts have been added.");

			if (_localizedPrompts.TryGetValue(language, out var prompt))
				return prompt;

			var fallbackLanguage = fallbackScheme.GetFallbackLanguage(language, _localizedPrompts.Keys);
			return _localizedPrompts[fallbackLanguage];
		}
	}
}