using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RCLargeLanguageModels.Locale;
using RCLargeLanguageModels.Metadata;
using RCLargeLanguageModels.Parsing;
using RCLargeLanguageModels.Prompting.Metadata;
using RCLargeLanguageModels.Prompting.Templates;

namespace RCLargeLanguageModels.Prompting
{
	/// <summary>
	/// Represents a library of templates that can be used to generate prompts and collections of messages.
	/// </summary>
	public partial class TemplateLibrary : IEnumerable<ITemplate>
	{
		private static readonly ConcurrentDictionary<string, ITemplateParser> _templateParsers = new();

		static TemplateLibrary()
		{
			_templateParsers.TryAdd("llt", new LLTParser());
		}

		/// <summary>
		/// Registers a template parser for the specified language.
		/// </summary>
		/// <param name="languageCode">The language code of the template language.</param>
		/// <param name="parser">The template parser to register.</param>
		/// <exception cref="ArgumentException">Thrown when a parser is already registered for the specified language code.</exception>
		public static void RegisterParser(string languageCode, ITemplateParser parser)
		{
			languageCode = languageCode.ToLowerInvariant();
			if (_templateParsers.ContainsKey(languageCode))
				throw new ArgumentException($"Parser already registered for language: '{languageCode}'");
			_templateParsers.TryAdd(languageCode, parser);
		}



		private readonly MultiValueDictionary<IMetadata, ITemplate> _templates = new();
		private readonly HashSet<ITemplate> _allTemplates = new();

		private readonly Dictionary<Type, MetadataFallbackScheme> _fallbackSchemes = new();
		private readonly Dictionary<Type, HashSet<IMetadata>> _fallbackMetadatas = new();

		private readonly object _lockObject = new object();

		/// <summary>
		/// Gets the shared template library instance.
		/// </summary>
		public static TemplateLibrary Shared { get; } = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateLibrary"/> class.
		/// </summary>
		public TemplateLibrary()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateLibrary"/> class.
		/// </summary>
		/// <param name="templates">The templates to add to the library.</param>
		public TemplateLibrary(IEnumerable<ITemplate> templates)
		{
			TryAddRange(templates);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateLibrary"/> class.
		/// </summary>
		/// <param name="languageFallbackScheme">The language fallback scheme to use. <see langword="null"/> uses the default scheme.</param>
		public TemplateLibrary(ILanguageFallbackScheme? languageFallbackScheme)
		{
			SetLanguageFallbackScheme(languageFallbackScheme ?? new MajorLanguageFallbackScheme());
		}

		/// <summary>
		/// Adds a template to the library and associates it with its metadata.
		/// </summary>
		/// <param name="template">The template to add. Cannot be <see langword="null"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="template"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown if a template exists in the library.</exception>
		public void Add(ITemplate template)
		{
			if (template == null)
				throw new ArgumentNullException(nameof(template));

			lock (_lockObject)
				if (!_allTemplates.Add(template))
					throw new ArgumentException("Template already exists in the library.", nameof(template));

			foreach (var metadata in template.Metadata)
			{
				lock (_lockObject)
				{
					var metadataType = metadata.GetType();
					if (!_fallbackMetadatas.TryGetValue(metadataType, out var fallbackMetadatas))
						_fallbackMetadatas[metadataType] = fallbackMetadatas = new HashSet<IMetadata>();
					fallbackMetadatas.Add(metadata);
				}

				_templates.Add(metadata, template);
			}
		}

		/// <summary>
		/// Tries to add a template to the library and associates it with its metadata.
		/// </summary>
		/// <param name="template">The template to add. Cannot be <see langword="null"/>.</param>
		/// <returns><see langword="true"/> if the template was added; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="template"/> is <see langword="null"/>.</exception>
		public bool TryAdd(ITemplate template)
		{
			if (template == null)
				throw new ArgumentNullException(nameof(template));

			lock (_lockObject)
				if (!_allTemplates.Add(template))
					return false;

			foreach (var metadata in template.Metadata)
			{
				lock (_lockObject)
				{
					var metadataType = metadata.GetType();
					if (!_fallbackMetadatas.TryGetValue(metadataType, out var fallbackMetadatas))
						_fallbackMetadatas[metadataType] = fallbackMetadatas = new HashSet<IMetadata>();
					fallbackMetadatas.Add(metadata);
				}

				_templates.Add(metadata, template);
			}

			return true;
		}



		/// <summary>
		/// Adds a template to the library and associates it with its metadata.
		/// </summary>
		/// <param name="templates">The templates to add. Cannot be <see langword="null"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="templates"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown if a template exists in the library.</exception>
		public void AddRange(IEnumerable<ITemplate> templates)
		{
			if (templates == null)
				throw new ArgumentNullException(nameof(templates));

			lock (_lockObject)
				foreach (var template in templates)
				{
					if (!_allTemplates.Add(template))
						throw new ArgumentException("Template already exists in the library.", nameof(template));

					foreach (var metadata in template.Metadata)
					{
						var metadataType = metadata.GetType();
						if (!_fallbackMetadatas.TryGetValue(metadataType, out var fallbackMetadatas))
							_fallbackMetadatas[metadataType] = fallbackMetadatas = new HashSet<IMetadata>();
						fallbackMetadatas.Add(metadata);

						_templates.Add(metadata, template);
					}
				}
		}

		/// <summary>
		/// Tries to add a range of templates to the library and associates them with their metadata.
		/// </summary>
		/// <param name="templates">The templates to add. Cannot be <see langword="null"/>.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="templates"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown if a template exists in the library.</exception>
		public void TryAddRange(IEnumerable<ITemplate> templates)
		{
			if (templates == null)
				throw new ArgumentNullException(nameof(templates));

			lock (_lockObject)
				foreach (var template in templates)
				{
					if (!_allTemplates.Add(template))
						continue;

					foreach (var metadata in template.Metadata)
					{
						var metadataType = metadata.GetType();
						if (!_fallbackMetadatas.TryGetValue(metadataType, out var fallbackMetadatas))
							_fallbackMetadatas[metadataType] = fallbackMetadatas = new HashSet<IMetadata>();
						fallbackMetadatas.Add(metadata);

						_templates.Add(metadata, template);
					}
				}
		}

		/// <summary>
		/// Sets the fallback scheme for the specified type of metadata.
		/// </summary>
		/// <param name="metadataType">The type of metadata for which to add the fallback scheme. Cannot be <see langword="null"/>.</param>
		/// <param name="scheme">The fallback scheme to add. Cannot be <see langword="null"/>.</param>
		public void SetFallbackScheme(Type metadataType, MetadataFallbackScheme scheme)
		{
			_fallbackSchemes[metadataType ?? throw new ArgumentNullException(nameof(metadataType))] =
				scheme ?? throw new ArgumentNullException(nameof(scheme));
		}

		/// <summary>
		/// Sets the fallback scheme for the specified type of metadata.
		/// </summary>
		/// <param name="scheme">The fallback scheme to add. Cannot be <see langword="null"/>.</param>
		public void SetFallbackScheme<Metadata>(MetadataFallbackScheme<Metadata> scheme)
			where Metadata : IMetadata
		{
			_fallbackSchemes[typeof(Metadata)] =
				scheme ?? throw new ArgumentNullException(nameof(scheme));
		}

		/// <summary>
		/// Sets the fallback scheme for the language metadata.
		/// </summary>
		/// <param name="scheme">The language fallback scheme to add. Cannot be <see langword="null"/>.</param>
		public void SetLanguageFallbackScheme(ILanguageFallbackScheme scheme)
		{
			_fallbackSchemes[typeof(LanguageMetadata)] =
				new LanguageMetadataFallbackScheme(scheme ?? throw new ArgumentNullException(nameof(scheme)));
		}

		/// <summary>
		/// Sets the fallback scheme for the language metadata.
		/// </summary>
		/// <param name="scheme">The language fallback scheme to add. Cannot be <see langword="null"/>.</param>
		public void SetLanguageFallbackScheme(LanguageMetadataFallbackScheme scheme)
		{
			_fallbackSchemes[typeof(LanguageMetadata)] =
				scheme ?? throw new ArgumentNullException(nameof(scheme));
		}



		public IEnumerator<ITemplate> GetEnumerator()
		{
			return _allTemplates.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}



		/// <summary>
		/// Imports a set of templates from the specified string contents.
		/// </summary>
		/// <param name="templateContents">The string contents containing the templates to import.</param>
		/// <param name="languageCode">The language code of the template language. Default is "llt", Large Language Template.</param>
		/// <exception cref="ArgumentException">Thrown when no parser is registered for the specified language code.</exception>
		/// <exception cref="ParsingException">Thrown when the template contents cannot be parsed.</exception>
		public void ImportFromString(string templateContents, string languageCode = "llt")
		{
			if (!_templateParsers.TryGetValue(languageCode, out var parser))
				throw new ArgumentException($"No parser registered for language: '{languageCode}'.");

			var templates = parser.Parse(templateContents);
			foreach (var template in templates)
				Add(template);
		}

		/// <summary>
		/// Imports a set of templates from the specified reader contents.
		/// </summary>
		/// <param name="reader">The reader containing the templates to import.</param>
		/// <param name="languageCode">The language code of the template language. Default is "llt", Large Language Template.</param>
		/// <exception cref="ArgumentException">Thrown when no parser is registered for the specified language code.</exception>
		/// <exception cref="ParsingException">Thrown when the template contents cannot be parsed.</exception>
		public void ImportFromReader(TextReader reader, string languageCode = "llt")
		{
			if (!_templateParsers.TryGetValue(languageCode, out var parser))
				throw new ArgumentException($"No parser registered for language: '{languageCode}'.");

			var templateContents = reader?.ReadToEnd() ?? throw new ArgumentNullException(nameof(reader));
			var templates = parser.Parse(templateContents);
			foreach (var template in templates)
				Add(template);
		}

		/// <summary>
		/// Imports a set of templates from the specified stream contents.
		/// </summary>
		/// <param name="stream">The stream containing the templates to import. Reads the entire content of the stream before parsing.</param>
		/// <param name="languageCode">The language code of the template language. Default is "llt", Large Language Template.</param>
		/// <exception cref="ArgumentException">Thrown when no parser is registered for the specified language code.</exception>
		/// <exception cref="ParsingException">Thrown when the template contents cannot be parsed.</exception>
		public void ImportFromStream(Stream stream, string languageCode = "llt")
		{
			if (!_templateParsers.TryGetValue(languageCode, out var parser))
				throw new ArgumentException($"No parser registered for language: '{languageCode}'.");

			using var reader = new StreamReader(stream);
			var templateContents = reader.ReadToEnd();
			var templates = parser.Parse(templateContents);
			foreach (var template in templates)
				Add(template);
		}

		/// <summary>
		/// Imports a set of templates from the specified library.
		/// </summary>
		/// <param name="library">The library containing the templates to import.</param>
		public void ImportFromLibrary(TemplateLibrary library)
		{
			TryAddRange(library);
		}

		/// <summary>
		/// Imports a set of templates from the specified file contents.
		/// </summary>
		/// <param name="filename">The file containing the templates to import. Extracts the language code from the file extension or uses "llt" when no extension is provided.</param>
		/// <exception cref="ArgumentException">Thrown when no parser is registered for the specified language code.</exception>
		/// <exception cref="ParsingException">Thrown when the template contents cannot be parsed.</exception>
		public void ImportFromFile(string filename)
		{
			var languageCode = Path.GetExtension(filename)?.TrimStart('.')?.ToLowerInvariant() ?? "llt";
			if (!_templateParsers.TryGetValue(languageCode, out var parser))
				throw new ArgumentException($"No parser registered for language: '{languageCode}'.");

			var templateContents = File.ReadAllText(filename);
			var templates = parser.Parse(templateContents);
			AddRange(templates);
		}

		/// <summary>
		/// Imports a set of templates from the specified file contents.
		/// </summary>
		/// <param name="filename">The file containing the templates to import.</param>
		/// <param name="languageCode">The language code of the template language. Default is "llt", Large Language Template.</param>
		/// <exception cref="ArgumentException">Thrown when no parser is registered for the specified language code.</exception>
		/// <exception cref="ParsingException">Thrown when the template contents cannot be parsed.</exception>
		public void ImportFromFile(string filename, string languageCode)
		{
			if (!_templateParsers.TryGetValue(languageCode, out var parser))
				throw new ArgumentException($"No parser registered for language: '{languageCode}'.");

			var templateContents = File.ReadAllText(filename);
			var templates = parser.Parse(templateContents);
			AddRange(templates);
		}

		/// <summary>
		/// Imports templates from folder.
		/// </summary>
		/// <param name="folderPath">The path of the folder containing the templates to import.</param>
		/// <param name="recursive">Whether to import templates from subfolders recursively. Default is <see langword="false"/>.</param>
		/// <returns>The collection of parsing exceptions encountered during the import process. If no exceptions were encountered, an empty collection is returned.</returns>
		public IEnumerable<ParsingException> ImportFromFolder(string folderPath, bool recursive = false)
		{
			return ImportFromFolder(folderPath, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
		}

		/// <summary>
		/// Imports templates from folder.
		/// </summary>
		/// <remarks>
		/// Imports only files with registered extesnions (or language codes), such as ".llt".
		/// </remarks>
		/// <param name="folderPath">The path of the folder containing the templates to import.</param>
		/// <param name="searchOption">The search option to use when searching for files in the folder.</param>
		/// <returns>The collection of parsing exceptions encountered during the import process. If no exceptions were encountered, an empty collection is returned.</returns>
		public IEnumerable<ParsingException> ImportFromFolder(string folderPath, SearchOption searchOption)
		{
			foreach (var file in Directory.GetFiles(folderPath, "*", searchOption))
			{
				var languageCode = Path.GetExtension(file)?.TrimStart('.')?.ToLowerInvariant();

				if (languageCode != null && _templateParsers.TryGetValue(languageCode, out var parser))
				{
					ParsingException? _ex = null;

					try
					{
						var templates = parser.Parse(File.ReadAllText(file));
						AddRange(templates);
					}
					catch (ParsingException ex)
					{
						_ex = ex;
					}

					if (_ex != null)
						yield return _ex;
				}
			}
			yield break;
		}

		/// <summary>
		/// Imports templates from assembly's manifest resources.
		/// </summary>
		/// <remarks>
		/// Imports only files with registered extesnions (or language codes), such as ".llt".
		/// </remarks>
		/// <param name="assembly">The assembly containing the templates to import.</param>
		/// <param name="folder">
		/// The folder within the assembly containing the templates to import. <br/>
		/// If <see langword="null"/>, imports all resources. <br/>
		/// Must be a valid manifest resource prefix in format: DefaultNamespace.FolderName.AnotherFolderName... <br/>
		/// </param>
		/// <returns></returns>
		public IEnumerable<ParsingException> ImportFromAssembly(Assembly assembly, string? folder = null)
		{
			foreach (var resource in assembly.GetManifestResourceNames())
			{
				if (folder != null && !resource.StartsWith(folder))
					continue;

				var languageCode = Path.GetFileNameWithoutExtension(resource)?.TrimStart('.')?.ToLowerInvariant();
				if (_templateParsers.TryGetValue(languageCode, out var parser))
				{
					ParsingException? _ex = null;

					try
					{
						var stream = assembly.GetManifestResourceStream(resource);
						using var reader = new StreamReader(stream);
						var templateContents = reader.ReadToEnd();
						var templates = parser.Parse(templateContents);
						AddRange(templates);
					}
					catch (ParsingException ex)
					{
						_ex = ex;
					}

					if (_ex != null)
						yield return _ex;
				}
			}
		}
	}
}