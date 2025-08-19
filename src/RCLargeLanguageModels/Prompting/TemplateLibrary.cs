using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCLargeLanguageModels.Locale;
using RCLargeLanguageModels.Metadata;
using RCLargeLanguageModels.Prompting.Metadata;
using RCLargeLanguageModels.Prompting.Templates;

namespace RCLargeLanguageModels.Prompting
{
	/// <summary>
	/// Represents a library of templates that can be used to generate prompts and collections of messages.
	/// </summary>
	public class TemplateLibrary : IEnumerable<ITemplate>
	{
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
			SetLanguageFallbackScheme(new MajorLanguageFallbackScheme());
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
		/// <exception cref="ArgumentException">Thrown if a template with the same metadata already exists in the library.</exception>
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

		private bool TryRetrieve(IMetadata metadata, out ICollection<ITemplate> templates)
		{
			lock (_lockObject)
			{
				if (_templates.TryGetValue(metadata, out templates))
					return true;

				var metadataType = metadata.GetType();
				if (_fallbackSchemes.TryGetValue(metadataType, out var scheme))
				{
					if (!_fallbackMetadatas.TryGetValue(metadataType, out var fallbackMetadatas))
						return false;

					var fallbackMetadata = scheme.GetFallbackMetadata(metadata, fallbackMetadatas);
					return _templates.TryGetValue(fallbackMetadata, out templates);
				}
			}

			return false;
		}

		/// <summary>
		/// Retrieves a first matching template based on the given metadata.
		/// </summary>
		/// <remarks>
		/// Returns the first template that matches (or contains in <see cref="ITemplate.Metadata"/>)
		/// the every given metadata object. If not, throws a <see cref="KeyNotFoundException"/>.
		/// </remarks>
		/// <param name="metadatas">
		/// The metadata to use for retrieving the template.
		/// Can be an array of metadata objects or a single metadata object.
		/// If multiple metadata objects are provided, they must all match exactly.
		/// </param>
		/// <returns>The template that matches all the given metadata.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="metadatas"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="metadatas"/> is an empty array.</exception>
		/// <exception cref="KeyNotFoundException">Thrown if no template matches all the given metadata.</exception>
		public ITemplate Retrieve(params IMetadata[] metadatas)
		{
			if (metadatas == null)
				throw new ArgumentNullException(nameof(metadatas));
			if (metadatas.Length == 0)
				throw new ArgumentException("At least one metadata must be provided for retrieval.", nameof(metadatas));

			HashSet<ITemplate>? result = null;

			foreach (var metadata in metadatas)
			{
				if (TryRetrieve(metadata, out var templates))
				{
					if (result == null)
					{
						result = new (templates);
					}
					else
					{
						lock (_lockObject)
							result.IntersectWith(templates);
						if (result.Count == 0)
							throw new KeyNotFoundException($"No templates found for metadata {metadata}.");
					}
				}
				else
				{
					throw new KeyNotFoundException($"No templates found for metadata {metadata}.");
				}
			}

			return result?.FirstOrDefault() ?? throw new KeyNotFoundException("No template found with the given metadata.");
		}

		/// <inheritdoc cref="Retrieve(IMetadata[])"/>
		/// <param name="identifier">The identifier of the template to retrieve.</param>
		/// <param name="metadatas"></param>
		public ITemplate Retrieve(string identifier, params IMetadata[] metadatas)
		{
			return Retrieve(metadatas.Prepend(new TemplateIdentifierMetadata(identifier)).ToArray());
		}

		/// <inheritdoc cref="Retrieve(IMetadata[])"/>
		/// <param name="identifier">The identifier of the template to retrieve.</param>
		public ITemplate Retrieve(string identifier)
		{
			return Retrieve(new TemplateIdentifierMetadata(identifier));
		}

		/// <summary>
		/// Retrieves a first matching template based on the given metadata.
		/// </summary>
		/// <remarks>
		/// Returns the first template that matches (or contains in <see cref="ITemplate.Metadata"/>)
		/// the every given metadata object. If not, returns <see langword="null"/>.
		/// </remarks>
		/// <param name="metadatas">
		/// The metadata to use for retrieving the template.
		/// Can be an array of metadata objects or a single metadata object.
		/// If multiple metadata objects are provided, they must all match exactly.
		/// </param>
		/// <returns>The template that matches all the given metadata or <see langword="null"/> if no template matches.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="metadatas"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="metadatas"/> is an empty array.</exception>
		public ITemplate? TryRetrieve(params IMetadata[] metadatas)
		{
			if (metadatas == null)
				throw new ArgumentNullException(nameof(metadatas));
			if (metadatas.Length == 0)
				throw new ArgumentException("At least one metadata must be provided for retrieval.", nameof(metadatas));

			HashSet<ITemplate>? result = null;

			foreach (var metadata in metadatas)
			{
				if (TryRetrieve(metadata, out var templates))
				{
					if (result == null)
					{
						result = new (templates);
					}
					else
					{
						lock (_lockObject)
							result.IntersectWith(templates);
						if (result.Count == 0)
							return null;
					}
				}
				else
				{
					return null;
				}
			}

			return result?.FirstOrDefault();
		}

		/// <inheritdoc cref="TryRetrieve(IMetadata[])"/>
		/// <param name="identifier">The identifier of the template to retrieve.</param>
		/// <param name="metadatas"></param>
		public ITemplate? TryRetrieve(string identifier, params IMetadata[] metadatas)
		{
			return TryRetrieve(metadatas.Prepend(new TemplateIdentifierMetadata(identifier)).ToArray());
		}

		/// <inheritdoc cref="TryRetrieve(IMetadata[])"/>
		/// <param name="identifier">The identifier of the template to retrieve.</param>
		public ITemplate? TryRetrieve(string identifier)
		{
			return TryRetrieve(new TemplateIdentifierMetadata(identifier));
		}

		/// <summary>
		/// Retrieves a first matching template based on the given metadata.
		/// If no exact match is found, returns the last successfully matched result.
		/// </summary>
		/// <remarks>
		/// This method performs a sequential matching of the provided metadata array. <br/>
		/// Order of metadata matters. <para/>
		/// 
		/// Algorithm: <br/>
		/// 1. Start with the first metadata in the array. <br/>
		/// 2. If there are templates for it, keep them as the current candidate set. <br/>
		/// 3. For the next metadata: <br/>
		///    - If intersection with the candidate set is non-empty, continue. <br/>
		///    - If intersection becomes empty, return the last candidate (best available). <br/>
		/// 4. If at some step no templates exist for a metadata key, stop and return the last candidate. <br/>
		/// 5. If the loop completes, return the final intersected template (or null if none). <para/>
		///
		/// Examples: <para/>
		/// 
		/// Input: [ID: sample_template, LANG: en_US] <br/>
		/// Available templates: <br/>
		/// - [ID: sample_template, LANG: zh_CN] <br/>
		/// Result: <br/>
		/// - Matches "ID: sample_template" first → candidate found. <br/>
		/// - Next metadata "LANG: en_US" → no intersection. <br/>
		/// - Returns last candidate → [ID: sample_template, LANG: zh_CN]. <para/>
		///
		/// Input: [LANG: en_US, ID: sample_template] <br/>
		/// Available templates: <br/>
		/// - [ID: sample_template, LANG: zh_CN] <br/>
		/// Result: <br/>
		/// - First metadata "LANG: en_US" → no candidates found. <br/>
		/// - Immediately returns null. <para/>
		/// 
		/// Input: [ID: order, TYPE: invoice, LANG: en] <br/>
		/// Available templates: <br/>
		/// - [ID: order, TYPE: invoice, LANG: fr] <br/>
		/// - [ID: order, TYPE: receipt, LANG: en] <br/>
		/// Result: <br/>
		/// - First metadata "ID: order" → 2 candidates. <br/>
		/// - Next metadata "TYPE: invoice" → narrows to [ID: order, TYPE: invoice, LANG: fr]. <br/>
		/// - Next metadata "LANG: en" → intersection empty. <br/>
		/// - Returns last candidate → [ID: order, TYPE: invoice, LANG: fr]. <para/>
		/// 
		/// This approach guarantees that at least some "best-effort" template is returned
		/// if the beginning of the metadata chain was matched, but later elements failed.
		/// </remarks>
		/// <param name="metadatas">The metadata to use for retrieving the template. Order matters.</param>
		/// <returns>
		/// The best-effort matching template, or <see langword="null"/> if no initial metadata matches.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="metadatas"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="metadatas"/> is an empty array.</exception>
		public ITemplate? TryRetrieveBest(params IMetadata[] metadatas)
		{
			if (metadatas == null)
				throw new ArgumentNullException(nameof(metadatas));
			if (metadatas.Length == 0)
				throw new ArgumentException("At least one metadata must be provided for retrieval.", nameof(metadatas));

			HashSet<ITemplate>? result = null;

			foreach (var metadata in metadatas)
			{
				if (TryRetrieve(metadata, out var templates))
				{
					if (result == null)
					{
						result = new (templates);
					}
					else
					{
						// If we've got a `templates`, it guaranteed to be not empty
						var res = result.First();
						lock (_lockObject)
							result.IntersectWith(templates);
						if (result.Count == 0)
							return res;
					}
				}
				else
				{
					break;
				}
			}

			return result?.FirstOrDefault();
		}

		/// <inheritdoc cref="TryRetrieveBest(IMetadata[])"/>
		/// <param name="identifier">The identifier of the template to retrieve.</param>
		/// <param name="metadatas"></param>
		public ITemplate? TryRetrieveBest(string identifier, params IMetadata[] metadatas)
		{
			return TryRetrieveBest(metadatas.Prepend(new TemplateIdentifierMetadata(identifier)).ToArray());
		}

		/// <inheritdoc cref="TryRetrieveBest(IMetadata[])"/>
		/// <param name="identifier">The identifier of the template to retrieve.</param>
		public ITemplate? TryRetrieveBest(string identifier)
		{
			return TryRetrieveBest(new TemplateIdentifierMetadata(identifier));
		}

		public IEnumerator<ITemplate> GetEnumerator()
		{
			return _allTemplates.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}