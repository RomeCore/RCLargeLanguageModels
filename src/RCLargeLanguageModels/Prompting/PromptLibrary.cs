using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCLargeLanguageModels.Locale;

namespace RCLargeLanguageModels.Prompting
{
	/// <summary>
	/// Represents a library of multilingual prompts.
	/// </summary>
	public class PromptLibrary : IEnumerable<KeyValuePair<string, MultilingualPrompt>>
	{
		private readonly Dictionary<string, MultilingualPrompt> _prompts;

		/// <summary>
		/// The shared instance of the <see cref="PromptLibrary"/> class with default prompts imported.
		/// </summary>
		public static PromptLibrary Shared { get; }

		static PromptLibrary()
		{
			Shared = new PromptLibrary();
		}

		/// <summary>
		/// Initializes a new empty instance of the <see cref="PromptLibrary"/> class.
		/// </summary>
		public PromptLibrary()
		{
			_prompts = new Dictionary<string, MultilingualPrompt>();
		}

		/// <summary>
		/// Adds a new prompt to the library.
		/// </summary>
		/// <param name="name">The name of the prompt.</param>
		/// <param name="prompt">The multilingual prompt to add.</param>
		/// <returns><see langword="true"/> if the prompt was added successfully; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException"></exception>
		public bool Add(string name, MultilingualPrompt prompt)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));
			if (prompt == null)
				throw new ArgumentNullException(nameof(prompt));

			if (_prompts.ContainsKey(name))
				return false;

			_prompts[name] = prompt;
			return true;
		}

		public bool Add(string name, LanguageCode languageCode, string prompt)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));
			if (string.IsNullOrEmpty(prompt))
				throw new ArgumentNullException(nameof(prompt));

			if (_prompts.TryGetValue(name, out var multilingualPrompt))
				return multilingualPrompt.Add(languageCode, prompt);

			multilingualPrompt = new MultilingualPrompt();
			return multilingualPrompt.Add(languageCode, prompt);
		}

		public void Set(string name, MultilingualPrompt prompt)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));
			if (prompt == null)
				throw new ArgumentNullException(nameof(prompt));

			_prompts[name] = prompt;
		}
		
		public void Set(string name, LanguageCode languageCode, string prompt)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));
			if (prompt == null)
				throw new ArgumentNullException(nameof(prompt));

			if (_prompts.TryGetValue(name, out var multilingualPrompt))
			{
				multilingualPrompt.Set(languageCode, prompt);
			}
			else
			{
				multilingualPrompt = new MultilingualPrompt();
				multilingualPrompt.Set(languageCode, prompt);
				_prompts[name] = multilingualPrompt;
			}
		}

		public bool Remove(string name)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			return _prompts.Remove(name);
		}

		public bool Remove(string name, LanguageCode languageCode)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if (_prompts.TryGetValue(name, out var multilingualPrompt))
				return multilingualPrompt.Remove(languageCode);

			return false;
		}

		public MultilingualPrompt GetPrompt(string name)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			if (_prompts.TryGetValue(name, out var multilingualPrompt))
				return multilingualPrompt;

			return null;
		}

		public string GetPrompt(string name, LanguageCode languageCode)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));
			if (_prompts.TryGetValue(name, out var multilingualPrompt))
				return multilingualPrompt.GetPrompt(languageCode);
			return null;
		}

		/// <summary>
		/// Gets the prompt names in the library.
		/// </summary>
		/// <returns>The prompt names collection.</returns>
		public IEnumerable<string> GetPromptNames() => _prompts.Keys;

		/// <summary>
		/// Gets the prompts in the library.
		/// </summary>
		/// <returns>The prompts collection.</returns>
		public IEnumerable<MultilingualPrompt> GetPrompts() => _prompts.Values;

		public IEnumerator<KeyValuePair<string, MultilingualPrompt>> GetEnumerator()
		{
			return _prompts.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}