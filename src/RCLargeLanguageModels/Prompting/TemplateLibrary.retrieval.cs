using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RCLargeLanguageModels.Metadata;
using RCLargeLanguageModels.Prompting.Metadata;
using RCLargeLanguageModels.Prompting.Templates;

namespace RCLargeLanguageModels.Prompting
{
	// Как же тут много 

	public partial class TemplateLibrary
	{
		private bool TryRetrieve(IMetadata metadata, bool useFallbackSchemes, out ICollection<ITemplate> templates)
		{
			lock (_lockObject)
			{
				if (_templates.TryGetValue(metadata, out templates))
					return true;

				if (useFallbackSchemes)
				{
					var metadataType = metadata.GetType();
					if (_fallbackSchemes.TryGetValue(metadataType, out var scheme))
					{
						if (!_fallbackMetadatas.TryGetValue(metadataType, out var fallbackMetadatas))
							return false;

						var fallbackMetadata = scheme.GetFallbackMetadata(metadata, fallbackMetadatas);
						return _templates.TryGetValue(fallbackMetadata, out templates);
					}
				}
			}

			return false;
		}

		private IEnumerable<ITemplate> Retrieve(IMetadata[] metadatas, bool useFallbackSchemes)
		{
			if (metadatas == null)
				throw new ArgumentNullException(nameof(metadatas));
			if (metadatas.Length == 0)
				throw new ArgumentException("At least one metadata must be provided for retrieval.", nameof(metadatas));

			HashSet<ITemplate>? result = null;

			foreach (var metadata in metadatas)
			{
				if (TryRetrieve(metadata, useFallbackSchemes, out var templates))
				{
					if (result == null)
					{
						result = new(templates);
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

			return result ?? throw new KeyNotFoundException("No template found with the given metadata.");
		}

		private IEnumerable<ITemplate>? TryRetrieve(IMetadata[] metadatas, bool useFallbackSchemes)
		{
			if (metadatas == null)
				throw new ArgumentNullException(nameof(metadatas));
			if (metadatas.Length == 0)
				throw new ArgumentException("At least one metadata must be provided for retrieval.", nameof(metadatas));

			HashSet<ITemplate>? result = null;

			foreach (var metadata in metadatas)
			{
				if (TryRetrieve(metadata, useFallbackSchemes, out var templates))
				{
					if (result == null)
					{
						result = new(templates);
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

			return result;
		}

		private IEnumerable<ITemplate>? TryRetrieveBest(IMetadata[] metadatas, bool useFallbackSchemes)
		{
			if (metadatas == null)
				throw new ArgumentNullException(nameof(metadatas));
			if (metadatas.Length == 0)
				throw new ArgumentException("At least one metadata must be provided for retrieval.", nameof(metadatas));

			HashSet<ITemplate>? result = null;

			foreach (var metadata in metadatas)
			{
				if (TryRetrieve(metadata, useFallbackSchemes, out var templates))
				{
					if (result == null)
					{
						result = new(templates);
					}
					else
					{
						// If we've got a `templates`, it guaranteed to be not empty
						var res = result.ToList();
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

			return result;
		}

		// ==== SINGLE, NO FALLBACKS ==== //

		/// <summary>
		/// Retrieves the first template that exactly matches all the provided metadata.
		/// </summary>
		/// <remarks>
		/// This method performs an exact sequential match of the provided metadata array. <br/>
		/// Order of metadata matters: each metadata object must be satisfied by a corresponding template. <para/>
		///
		/// Algorithm: <br/>
		/// 1. Start with the first metadata in the array. <br/>
		/// 2. Retrieve all templates associated with this metadata. <br/>
		/// 3. For each subsequent metadata:
		///    - Intersect the current candidate set with templates matching the next metadata. <br/>
		///    - If the intersection becomes empty, a <see cref="KeyNotFoundException"/> is thrown because no exact match exists. <br/>
		/// 4. If all metadata objects are successfully matched, the first template from the intersection set is returned. <para/>
		///
		/// Examples: <para/>
		/// Input: [ID: sample_template, LANG: en_US] <br/>
		/// Available templates: <br/>
		/// - [ID: sample_template, LANG: en_US] <br/>
		/// - [ID: sample_template, LANG: zh_CN] <br/>
		/// Result: <br/>
		/// - First metadata "ID: sample_template" → candidates found: 2 templates. <br/>
		/// - Next metadata "LANG: en_US" → intersection yields [ID: sample_template, LANG: en_US]. <br/>
		/// - Returns the first template from the intersected set. <para/>
		///
		/// Input: [ID: sample_template, LANG: fr_FR] <br/>
		/// Available templates: <br/>
		/// - [ID: sample_template, LANG: en_US] <br/>
		/// Result: <br/>
		/// - First metadata "ID: sample_template" → candidate found. <br/>
		/// - Next metadata "LANG: fr_FR" → intersection empty → throws <see cref="KeyNotFoundException"/>. <para/>
		///
		/// This approach guarantees that only an exact match of all provided metadata objects is returned.
		/// No fallback or partial match is used.
		/// </remarks>
		/// <param name="metadatas">
		/// The metadata to use for retrieving the template.
		/// Can be a single metadata object or an array of metadata objects.
		/// If multiple metadata objects are provided, they must all match exactly.
		/// </param>
		/// <returns>
		/// The first template that matches all the provided metadata.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="metadatas"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="metadatas"/> is an empty array.</exception>
		/// <exception cref="KeyNotFoundException">Thrown if no template matches all the provided metadata.</exception>
		public ITemplate Retrieve(params IMetadata[] metadatas)
		{
			return Retrieve(metadatas, useFallbackSchemes: false).First();
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
		/// Retrieves the first matching template based on the given metadata, without using fallback schemes.
		/// </summary>
		/// <remarks>
		/// This method performs a sequential check for templates that match all provided metadata objects exactly. <br/>
		/// Order of metadata matters, and all metadata objects must be matched for a template to be considered a valid result. <para/>
		/// 
		/// Algorithm: <br/>
		/// 1. Start with the first metadata in the array. <br/>
		/// 2. If templates exist for it, keep them as the current candidate set. <br/>
		/// 3. For each subsequent metadata: <br/>
		///    - Intersect the candidate set with templates matching the current metadata. <br/>
		///    - If intersection becomes empty at any step, return <see langword="null"/>. <br/>
		/// 4. If all metadata objects are successfully matched, return the first template from the candidate set. <para/>
		/// 
		/// Examples: <para/>
		/// Input: [ID: sample_template, LANG: en_US] <br/>
		/// Available templates: <br/>
		/// - [ID: sample_template, LANG: en_US] <br/>
		/// Result: <br/>
		/// - Matches both ID and LANG → returns the template [ID: sample_template, LANG: en_US]. <para/>
		///
		/// Input: [LANG: fr_FR, ID: sample_template] <br/>
		/// Available templates: <br/>
		/// - [ID: sample_template, LANG: en_US] <br/>
		/// Result: <br/>
		/// - First metadata "LANG: fr_FR" → no candidates found → returns <see langword="null"/>. <para/>
		///
		/// Input: [ID: order, TYPE: invoice] <br/>
		/// Available templates: <br/>
		/// - [ID: order, TYPE: invoice] <br/>
		/// - [ID: order, TYPE: receipt] <br/>
		/// Result: <br/>
		/// - First metadata "ID: order" → 2 candidates. <br/>
		/// - Next metadata "TYPE: invoice" → narrows to [ID: order, TYPE: invoice] → returns this template. <para/>
		/// 
		/// This approach guarantees that only exact matches are returned, without applying fallback logic.
		/// </remarks>
		/// <param name="metadatas">
		/// The metadata to use for retrieving the template. Order matters.
		/// Can be an array of metadata objects or a single metadata object.
		/// All metadata objects must match exactly for a template to be returned.
		/// </param>
		/// <returns>
		/// The first template that matches all the given metadata, or <see langword="null"/> if no template matches exactly.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="metadatas"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="metadatas"/> is an empty array.</exception>
		public ITemplate? TryRetrieve(params IMetadata[] metadatas)
		{
			return TryRetrieve(metadatas, useFallbackSchemes: false)?.FirstOrDefault();
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
			return TryRetrieveBest(metadatas, useFallbackSchemes: false)?.FirstOrDefault();
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

		// ==== SINGLE, FALLBACKS ==== //

		/// <summary>
		/// Retrieves the first template that matches all the given metadata, using fallback schemes when necessary.
		/// If no exact match is found, returns the best-effort template based on available fallback metadata.
		/// </summary>
		/// <remarks>
		/// This method performs sequential metadata matching with fallback support. <br/>
		/// Order of metadata matters. <para/>
		/// 
		/// Algorithm: <br/>
		/// 1. Start with the first metadata in the array. <br/>
		/// 2. If there are templates for it, keep them as the current candidate set. <br/>
		/// 3. For the next metadata: <br/>
		///    - Attempt to intersect the candidate set with templates matching this metadata. <br/>
		///    - If intersection is non-empty, continue. <br/>
		///    - If intersection becomes empty, fallback metadata is used and the last candidate set is returned. <br/>
		/// 4. If at any step a metadata key has no templates, fallback metadata is applied and the last candidate set is returned. <br/>
		/// 5. If the loop completes successfully, the first template from the final candidate set is returned. <para/>
		///
		/// Examples: <para/>
		/// Input: [ID: sample_template, LANG: fr_FR] <br/>
		/// Available templates: <br/>
		/// - [ID: sample_template, LANG: en_US] <br/>
		/// Result: <br/>
		/// - First metadata "ID: sample_template" → candidate found. <br/>
		/// - Next metadata "LANG: fr_FR" → no intersection, fallback used → returns [ID: sample_template, LANG: en_US]. <para/>
		///
		/// Input: [LANG: de_DE, ID: sample_template] <br/>
		/// Available templates: <br/>
		/// - [ID: sample_template, LANG: en_US] <br/>
		/// Result: <br/>
		/// - First metadata "LANG: de_DE" → no candidates, fallback applied → returns [ID: sample_template, LANG: en_US]. <para/>
		///
		/// Input: [ID: order, TYPE: invoice, LANG: fr_FR] <br/>
		/// Available templates: <br/>
		/// - [ID: order, TYPE: invoice, LANG: en_US] <br/>
		/// - [ID: order, TYPE: receipt, LANG: fr_FR] <br/>
		/// Result: <br/>
		/// - First metadata "ID: order" → 2 candidates. <br/>
		/// - Next metadata "TYPE: invoice" → narrows to [ID: order, TYPE: invoice, LANG: en_US]. <br/>
		/// - Next metadata "LANG: fr_FR" → no intersection, fallback used → returns [ID: order, TYPE: invoice, LANG: en_US]. <para/>
		///
		/// This approach ensures that at least one meaningful template is returned, leveraging fallback schemes
		/// when exact metadata matches are not available. It guarantees the first available template from the
		/// best-effort intersection is returned.
		/// </remarks>
		/// <param name="metadatas">The metadata to use for retrieving the template. Order matters.</param>
		/// <returns>The first template that matches all metadata using fallback schemes.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="metadatas"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="metadatas"/> is empty.</exception>
		/// <exception cref="KeyNotFoundException">Thrown if no template matches even after applying fallback schemes.</exception>
		public ITemplate RetrieveWithFallback(params IMetadata[] metadatas)
		{
			return Retrieve(metadatas, useFallbackSchemes: true).First();
		}

		/// <inheritdoc cref="RetrieveWithFallback(IMetadata[])"/>
		/// <param name="identifier">The identifier of the template to retrieve.</param>
		/// <param name="metadatas">Additional metadata to refine the template selection.</param>
		public ITemplate RetrieveWithFallback(string identifier, params IMetadata[] metadatas)
		{
			return RetrieveWithFallback(metadatas.Prepend(new TemplateIdentifierMetadata(identifier)).ToArray());
		}

		/// <inheritdoc cref="RetrieveWithFallback(IMetadata[])"/>
		/// <param name="identifier">The identifier of the template to retrieve.</param>
		public ITemplate RetrieveWithFallback(string identifier)
		{
			return RetrieveWithFallback(new TemplateIdentifierMetadata(identifier));
		}

		/// <summary>
		/// Attempts to retrieve the first template that matches all provided metadata using fallback schemes.
		/// If an exact match is not found, fallback metadata may be used to return the best available template.
		/// </summary>
		/// <remarks>
		/// This method performs sequential metadata matching with support for fallback schemes. <br/>
		/// The order of metadata objects is significant. <para/>
		/// 
		/// Algorithm: <br/>
		/// 1. Start with the first metadata in the array. <br/>
		/// 2. Check if templates exist for this metadata. If found, keep them as the candidate set. <br/>
		/// 3. For each subsequent metadata: <br/>
		///    - Attempt to intersect candidate set with templates matching this metadata. <br/>
		///    - If intersection is non-empty, continue. <br/>
		///    - If intersection is empty, fallback schemes are applied to find an alternative template. <br/>
		/// 4. If at any step no templates exist for a metadata key, fallback may provide a best-effort candidate. <br/>
		/// 5. Returns the first template from the final candidate set, or <see langword="null"/> if no suitable template is found. <para/>
		/// 
		/// Examples: <para/>
		/// Input: [ID: sample_template, LANG: fr_FR] <br/>
		/// Available templates: <br/>
		/// - [ID: sample_template, LANG: en_US] <br/>
		/// Result: <br/>
		/// - First metadata "ID: sample_template" → candidate found. <br/>
		/// - Next metadata "LANG: fr_FR" → no exact match, fallback applied → returns [ID: sample_template, LANG: en_US]. <para/>
		///
		/// Input: [ID: order, TYPE: invoice, LANG: fr_FR] <br/>
		/// Available templates: <br/>
		/// - [ID: order, TYPE: invoice, LANG: en_US] <br/>
		/// - [ID: order, TYPE: receipt, LANG: fr_FR] <br/>
		/// Result: <br/>
		/// - First metadata "ID: order" → 2 candidates. <br/>
		/// - Next metadata "TYPE: invoice" → narrows to [ID: order, TYPE: invoice, LANG: en_US]. <br/>
		/// - Next metadata "LANG: fr_FR" → no intersection, fallback applied → returns [ID: order, TYPE: invoice, LANG: en_US]. <para/>
		/// 
		/// This approach ensures that at least one template is returned whenever possible,
		/// leveraging fallback metadata when exact matches are unavailable.
		/// </remarks>
		/// <param name="metadatas">The metadata to use for retrieving the template. Order matters.</param>
		/// <returns>The first matching template using fallback schemes, or <see langword="null"/> if no template is found.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="metadatas"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="metadatas"/> is empty.</exception>
		public ITemplate? TryRetrieveWithFallback(params IMetadata[] metadatas)
		{
			return TryRetrieve(metadatas, useFallbackSchemes: true)?.FirstOrDefault();
		}

		/// <inheritdoc cref="TryRetrieveWithFallback(IMetadata[])"/>
		/// <param name="identifier">The identifier of the template to retrieve.</param>
		/// <param name="metadatas">Additional metadata to refine the template selection.</param>
		public ITemplate? TryRetrieveWithFallback(string identifier, params IMetadata[] metadatas)
		{
			return TryRetrieveWithFallback(metadatas.Prepend(new TemplateIdentifierMetadata(identifier)).ToArray());
		}

		/// <inheritdoc cref="TryRetrieveWithFallback(IMetadata[])"/>
		/// <param name="identifier">The identifier of the template to retrieve.</param>
		public ITemplate? TryRetrieveWithFallback(string identifier)
		{
			return TryRetrieveWithFallback(new TemplateIdentifierMetadata(identifier));
		}

		/// <summary>
		/// Retrieves the best matching template based on the given metadata using fallback schemes.
		/// If no exact match is found, returns the last successfully matched candidate, potentially using fallback metadata.
		/// </summary>
		/// <remarks>
		/// This method performs a sequential matching of the provided metadata array with fallback support. <br/>
		/// Order of metadata matters. <para/>
		/// 
		/// Algorithm: <br/>
		/// 1. Start with the first metadata in the array. <br/>
		/// 2. If there are templates for it, keep them as the current candidate set. <br/>
		/// 3. For the next metadata: <br/>
		///    - Attempt to intersect with the candidate set. <br/>
		///    - If intersection is non-empty, continue to the next metadata. <br/>
		///    - If intersection becomes empty, the last successful candidate set is returned (best-effort match). <br/>
		/// 4. If a metadata key has no associated templates, the last candidate set is returned (possibly using fallback schemes). <br/>
		/// 5. If the loop completes successfully, the final intersection set is returned. <para/>
		/// 
		/// Fallback support allows returning templates with alternate metadata when an exact match is not available. <br/>
		/// For example, if a French template is requested but unavailable, an English template might be returned as a fallback. <para/>
		/// 
		/// Examples: <para/>
		/// Input: [ID: sample_template, LANG: fr_FR] <br/>
		/// Available templates: <br/>
		/// - [ID: sample_template, LANG: en_US] <br/>
		/// Result: <br/>
		/// - First metadata "ID: sample_template" → candidate found. <br/>
		/// - Next metadata "LANG: fr_FR" → no intersection, fallback used → returns [ID: sample_template, LANG: en_US]. <para/>
		///
		/// Input: [LANG: de_DE, ID: sample_template] <br/>
		/// Available templates: <br/>
		/// - [ID: sample_template, LANG: en_US] <br/>
		/// Result: <br/>
		/// - First metadata "LANG: de_DE" → no candidates, fallback used → returns [ID: sample_template, LANG: en_US]. <para/>
		///
		/// Input: [ID: order, TYPE: invoice, LANG: fr_FR] <br/>
		/// Available templates: <br/>
		/// - [ID: order, TYPE: invoice, LANG: en_US] <br/>
		/// - [ID: order, TYPE: receipt, LANG: fr_FR] <br/>
		/// Result: <br/>
		/// - First metadata "ID: order" → 2 candidates. <br/>
		/// - Next metadata "TYPE: invoice" → narrows to [ID: order, TYPE: invoice, LANG: en_US]. <br/>
		/// - Next metadata "LANG: fr_FR" → no intersection, fallback used → returns [ID: order, TYPE: invoice, LANG: en_US]. <para/>
		/// 
		/// This approach guarantees that at least some "best-effort" template is returned
		/// even if exact metadata matches are not available, leveraging fallback schemes when necessary.
		/// </remarks>
		/// <param name="metadatas">The metadata to use for retrieving the template. Order matters.</param>
		/// <returns>
		/// The best-effort matching template using fallback, or <see langword="null"/> if no initial metadata matches even with fallback.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="metadatas"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="metadatas"/> is an empty array.</exception>
		public ITemplate? TryRetrieveBestWithFallback(params IMetadata[] metadatas)
		{
			return TryRetrieveBest(metadatas, useFallbackSchemes: true)?.FirstOrDefault();
		}

		/// <inheritdoc cref="TryRetrieveBestWithFallback(IMetadata[])"/>
		/// <param name="identifier">The identifier of the template to retrieve.</param>
		/// <param name="metadatas">Additional metadata to refine the template selection.</param>
		public ITemplate? TryRetrieveBestWithFallback(string identifier, params IMetadata[] metadatas)
		{
			return TryRetrieveBestWithFallback(metadatas.Prepend(new TemplateIdentifierMetadata(identifier)).ToArray());
		}

		/// <inheritdoc cref="TryRetrieveBestWithFallback(IMetadata[])"/>
		/// <param name="identifier">The identifier of the template to retrieve.</param>
		public ITemplate? TryRetrieveBestWithFallback(string identifier)
		{
			return TryRetrieveBestWithFallback(new TemplateIdentifierMetadata(identifier));
		}

		// ==== COLLECTION, NO FALLBACKS ==== //

		/// <summary>
		/// Retrieves all templates that exactly match all of the given metadata.
		/// </summary>
		/// <remarks>
		/// This method performs sequential metadata matching without using fallback schemes. <br/>
		/// Only templates that satisfy **all provided metadata** are returned. <para/>
		/// 
		/// Algorithm: <br/>
		/// 1. Start with the first metadata in the array. <br/>
		/// 2. Find all templates associated with that metadata. <br/>
		/// 3. For each subsequent metadata, intersect the current candidate set with templates matching the next metadata. <br/>
		/// 4. If at any step the intersection becomes empty, a <see cref="KeyNotFoundException"/> is thrown, since no template matches all metadata. <br/>
		/// 5. If all metadata are successfully processed, the resulting candidate set contains all matching templates. <para/>
		///
		/// Examples: <para/>
		/// Input: [ID: sample_template, LANG: en_US] <br/>
		/// Available templates: <br/>
		/// - [ID: sample_template, LANG: en_US] <br/>
		/// - [ID: sample_template, LANG: zh_CN] <br/>
		/// Result: <br/>
		/// - Matches both ID and LANG → returns [ID: sample_template, LANG: en_US]. <para/>
		///
		/// Input: [ID: sample_template, LANG: fr_FR] <br/>
		/// Available templates: <br/>
		/// - [ID: sample_template, LANG: en_US] <br/>
		/// Result: <br/>
		/// - No template matches both ID and LANG → throws <see cref="KeyNotFoundException"/>. <para/>
		///
		/// Input: [TYPE: invoice] <br/>
		/// Available templates: <br/>
		/// - [ID: order, TYPE: invoice, LANG: en_US] <br/>
		/// - [ID: order, TYPE: invoice, LANG: fr_FR] <br/>
		/// Result: <br/>
		/// - Both templates match TYPE → returns both templates. <para/>
		///
		/// This approach guarantees that only templates fully matching the provided metadata are returned,
		/// without using fallback or best-effort matching.
		/// </remarks>
		/// <param name="metadatas">The metadata to use for retrieving templates. Order does not affect results.</param>
		/// <returns>An enumerable of templates matching all metadata.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="metadatas"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="metadatas"/> is an empty array.</exception>
		/// <exception cref="KeyNotFoundException">Thrown if no templates match all provided metadata.</exception>
		public IEnumerable<ITemplate> RetrieveAll(params IMetadata[] metadatas)
		{
			return Retrieve(metadatas, useFallbackSchemes: false);
		}

		/// <inheritdoc cref="RetrieveAll(IMetadata[])"/>
		/// <param name="identifier">The identifier of the template to retrieve.</param>
		/// <param name="metadatas">Additional metadata to refine the template selection.</param>
		public IEnumerable<ITemplate> RetrieveAll(string identifier, params IMetadata[] metadatas)
		{
			return RetrieveAll(metadatas.Prepend(new TemplateIdentifierMetadata(identifier)).ToArray());
		}

		/// <inheritdoc cref="RetrieveAll(IMetadata[])"/>
		/// <param name="identifier">The identifier of the template to retrieve.</param>
		public IEnumerable<ITemplate> RetrieveAll(string identifier)
		{
			return RetrieveAll(new TemplateIdentifierMetadata(identifier));
		}

		/// <summary>
		/// Attempts to retrieve all templates that match the given metadata exactly.
		/// If multiple metadata objects are provided, only templates that match all provided metadata are returned.
		/// </summary>
		/// <remarks>
		/// This method performs a sequential intersection of templates for each metadata item in the provided array. <br/>
		/// Order of metadata matters: templates must satisfy all metadata constraints to be included in the result. <para/>
		/// 
		/// Algorithm: <br/>
		/// 1. Start with the first metadata in the array. <br/>
		/// 2. Retrieve all templates associated with this metadata. <br/>
		/// 3. For each subsequent metadata, intersect its templates with the current candidate set. <br/>
		/// 4. If at any point the intersection is empty, the method returns <see langword="null"/> (no templates satisfy all metadata). <br/>
		/// 5. If all metadata are processed, returns the final set of matching templates (may contain multiple templates). <para/>
		/// 
		/// Examples: <para/>
		/// Input: [ID: invoice, LANG: en_US] <br/>
		/// Available templates: <br/>
		/// - [ID: invoice, LANG: en_US] <br/>
		/// - [ID: invoice, LANG: fr_FR] <br/>
		/// Result: <br/>
		/// - Returns: [ID: invoice, LANG: en_US] <para/>
		///
		/// Input: [ID: report, TYPE: summary] <br/>
		/// Available templates: <br/>
		/// - [ID: report, TYPE: summary, LANG: en] <br/>
		/// - [ID: report, TYPE: detail, LANG: en] <br/>
		/// Result: <br/>
		/// - Returns: [ID: report, TYPE: summary, LANG: en] <para/>
		///
		/// Input: [ID: order, LANG: de_DE] <br/>
		/// Available templates: <br/>
		/// - [ID: order, LANG: en_US] <br/>
		/// Result: <br/>
		/// - Returns: <see langword="null"/> (no exact match found) <para/>
		/// 
		/// This method does not use fallback schemes. If no exact match exists, <see langword="null"/> is returned.
		/// </remarks>
		/// <param name="metadatas">The metadata to use for retrieval. All metadata must match exactly for a template to be included.</param>
		/// <returns>
		/// An enumerable of templates matching all provided metadata, or <see langword="null"/> if no templates match.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="metadatas"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="metadatas"/> is an empty array.</exception>
		public IEnumerable<ITemplate>? TryRetrieveAll(params IMetadata[] metadatas)
		{
			return TryRetrieve(metadatas, useFallbackSchemes: false);
		}

		/// <inheritdoc cref="TryRetrieveAll(IMetadata[])"/>
		/// <param name="identifier">The identifier of the template to retrieve.</param>
		/// <param name="metadatas">Additional metadata to refine the template selection.</param>
		public IEnumerable<ITemplate>? TryRetrieveAll(string identifier, params IMetadata[] metadatas)
		{
			return TryRetrieveAll(metadatas.Prepend(new TemplateIdentifierMetadata(identifier)).ToArray());
		}

		/// <inheritdoc cref="TryRetrieveAll(IMetadata[])"/>
		/// <param name="identifier">The identifier of the template to retrieve.</param>
		public IEnumerable<ITemplate>? TryRetrieveAll(string identifier)
		{
			return TryRetrieveAll(new TemplateIdentifierMetadata(identifier));
		}

		/// <summary>
		/// Retrieves the best-effort matching templates for the given metadata.
		/// If an exact intersection of all metadata fails, returns the last successfully matched candidate set.
		/// </summary>
		/// <remarks>
		/// This method performs sequential metadata matching without using fallback schemes. <br/>
		/// Order of metadata matters. <para/>
		///
		/// Algorithm: <br/>
		/// 1. Start with the first metadata in the array. <br/>
		/// 2. If there are templates for it, keep them as the current candidate set. <br/>
		/// 3. For each subsequent metadata: <br/>
		///    - Intersect with the current candidate set. <br/>
		///    - If intersection is non-empty, continue to the next metadata. <br/>
		///    - If intersection becomes empty, return the last successful candidate set (best-effort match). <br/>
		/// 4. If a metadata key has no associated templates, stop and return the last candidate set. <br/>
		/// 5. If the loop completes successfully, return the final intersection set. <para/>
		///
		/// Examples: <para/>
		///
		/// Input: [ID: sample_template, LANG: en_US] <br/>
		/// Available templates: <br/>
		/// - [ID: sample_template, LANG: zh_CN] <br/>
		/// Result: <br/>
		/// - First metadata "ID: sample_template" → candidate found. <br/>
		/// - Next metadata "LANG: en_US" → intersection empty. <br/>
		/// - Returns last candidate set → [ID: sample_template, LANG: zh_CN]. <para/>
		///
		/// Input: [LANG: en_US, ID: sample_template] <br/>
		/// Available templates: <br/>
		/// - [ID: sample_template, LANG: zh_CN] <br/>
		/// Result: <br/>
		/// - First metadata "LANG: en_US" → no candidates. <br/>
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
		/// - Returns last candidate set → [ID: order, TYPE: invoice, LANG: fr]. <para/>
		///
		/// This approach guarantees that at least some "best-effort" template set is returned
		/// if the beginning of the metadata chain matches, even if later elements do not.
		/// </remarks>
		/// <param name="metadatas">The metadata to use for retrieving templates. Order matters.</param>
		/// <returns>
		/// An enumerable of templates representing the best-effort match, or <see langword="null"/> if no initial metadata matches.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="metadatas"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="metadatas"/> is an empty array.</exception>
		public IEnumerable<ITemplate>? TryRetrieveBestAll(params IMetadata[] metadatas)
		{
			return TryRetrieveBest(metadatas, useFallbackSchemes: false);
		}

		/// <inheritdoc cref="TryRetrieveBestAll(IMetadata[])"/>
		/// <param name="identifier">The identifier of the template to retrieve.</param>
		/// <param name="metadatas">Additional metadata to refine the template selection.</param>
		public IEnumerable<ITemplate>? TryRetrieveBestAll(string identifier, params IMetadata[] metadatas)
		{
			return TryRetrieveBestAll(metadatas.Prepend(new TemplateIdentifierMetadata(identifier)).ToArray());
		}

		/// <inheritdoc cref="TryRetrieveBestAll(IMetadata[])"/>
		/// <param name="identifier">The identifier of the template to retrieve.</param>
		public IEnumerable<ITemplate>? TryRetrieveBestAll(string identifier)
		{
			return TryRetrieveBestAll(new TemplateIdentifierMetadata(identifier));
		}

		// ==== COLLECTION, FALLBACKS ==== //

		/// <summary>
		/// Retrieves all templates that match all of the given metadata using fallback schemes.
		/// If an exact intersection of all metadata is not found, returns the last successfully matched candidate set using fallback metadata.
		/// </summary>
		/// <remarks>
		/// This method performs sequential matching of the provided metadata array with fallback support. <br/>
		/// Order of metadata matters. <para/>
		/// 
		/// Algorithm: <br/>
		/// 1. Start with the first metadata in the array. <br/>
		/// 2. If there are templates for it, keep them as the current candidate set. <br/>
		/// 3. For each subsequent metadata: <br/>
		///    - Attempt to intersect the candidate set with templates matching the current metadata. <br/>
		///    - If intersection is non-empty, continue to the next metadata. <br/>
		///    - If intersection becomes empty, the last successful candidate set is returned (best-effort match). <br/>
		/// 4. If a metadata key has no templates, fallback schemes are applied to find alternative metadata values. <br/>
		/// 5. If no candidates remain after applying all metadata (including fallback), a <see cref="KeyNotFoundException"/> is thrown. <para/>
		/// 
		/// Fallback support allows returning templates with alternate metadata when exact matches are not available. <br/>
		/// For example, if a French template is requested but unavailable, an English template may be returned as a fallback. <para/>
		/// 
		/// Examples: <para/>
		/// Input: [ID: sample_template, LANG: fr_FR] <br/>
		/// Available templates: <br/>
		/// - [ID: sample_template, LANG: en_US] <br/>
		/// Result: <br/>
		/// - First metadata "ID: sample_template" → candidate found. <br/>
		/// - Next metadata "LANG: fr_FR" → no intersection, fallback used → returns [ID: sample_template, LANG: en_US]. <para/>
		///
		/// Input: [ID: order, TYPE: invoice, LANG: fr_FR] <br/>
		/// Available templates: <br/>
		/// - [ID: order, TYPE: invoice, LANG: en_US] <br/>
		/// - [ID: order, TYPE: receipt, LANG: fr_FR] <br/>
		/// Result: <br/>
		/// - First metadata "ID: order" → 2 candidates. <br/>
		/// - Next metadata "TYPE: invoice" → narrows to [ID: order, TYPE: invoice, LANG: en_US]. <br/>
		/// - Next metadata "LANG: fr_FR" → no intersection, fallback used → returns [ID: order, TYPE: invoice, LANG: en_US]. <para/>
		/// 
		/// This approach guarantees that at least some "best-effort" templates are returned
		/// if the beginning of the metadata chain is matched, even if later elements fail.
		/// </remarks>
		/// <param name="metadatas">The metadata to use for retrieving templates. Order matters.</param>
		/// <returns>
		/// An enumerable of templates that best match all the given metadata using fallback.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="metadatas"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="metadatas"/> is empty.</exception>
		/// <exception cref="KeyNotFoundException">Thrown if no templates match even after applying fallback.</exception>
		public IEnumerable<ITemplate> RetrieveAllWithFallback(params IMetadata[] metadatas)
		{
			var result = Retrieve(metadatas, useFallbackSchemes: true);
			return result;
		}

		/// <inheritdoc cref="RetrieveAllWithFallback(IMetadata[])"/>
		/// <param name="identifier">The identifier of the template to retrieve.</param>
		/// <param name="metadatas">Additional metadata to refine the template selection.</param>
		public IEnumerable<ITemplate> RetrieveAllWithFallback(string identifier, params IMetadata[] metadatas)
		{
			return RetrieveAllWithFallback(metadatas.Prepend(new TemplateIdentifierMetadata(identifier)).ToArray());
		}

		/// <inheritdoc cref="RetrieveAllWithFallback(IMetadata[])"/>
		/// <param name="identifier">The identifier of the template to retrieve.</param>
		public IEnumerable<ITemplate> RetrieveAllWithFallback(string identifier)
		{
			return RetrieveAllWithFallback(new TemplateIdentifierMetadata(identifier));
		}

		/// <summary>
		/// Attempts to retrieve all templates that match the given metadata using fallback schemes.
		/// If no exact matches are found, returns the best-effort candidate templates based on fallback metadata.
		/// </summary>
		/// <remarks>
		/// This method performs sequential matching of the provided metadata array with fallback support. <br/>
		/// Order of metadata matters. <para/>
		///
		/// Algorithm: <br/>
		/// 1. Start with the first metadata in the array. <br/>
		/// 2. If templates exist for it, keep them as the current candidate set. <br/>
		/// 3. For each subsequent metadata: <br/>
		///    - Intersect with the current candidate set. <br/>
		///    - If intersection is non-empty, continue. <br/>
		///    - If intersection becomes empty, return the last successful candidate set (best-effort match). <br/>
		/// 4. If a metadata key has no associated templates, fallback schemes are applied to find alternatives. <br/>
		/// 5. If no candidates are found even after fallback, returns <see langword="null"/>. <para/>
		///
		/// Fallback ensures that templates with alternate metadata (e.g., different language or model) are considered
		/// when an exact match is unavailable. <para/>
		///
		/// Examples: <para/>
		/// Input: [ID: sample_template, LANG: fr_FR] <br/>
		/// Available templates: <br/>
		/// - [ID: sample_template, LANG: en_US] <br/>
		/// Result: <br/>
		/// - "ID: sample_template" matches → candidate set initialized. <br/>
		/// - "LANG: fr_FR" not available → fallback applied → returns [ID: sample_template, LANG: en_US]. <para/>
		///
		/// Input: [TYPE: invoice, LANG: fr_FR] <br/>
		/// Available templates: <br/>
		/// - [TYPE: invoice, LANG: en_US] <br/>
		/// - [TYPE: receipt, LANG: fr_FR] <br/>
		/// Result: <br/>
		/// - "TYPE: invoice" → candidate set: [TYPE: invoice, LANG: en_US] <br/>
		/// - "LANG: fr_FR" → no intersection → fallback used → returns [TYPE: invoice, LANG: en_US]. <para/>
		///
		/// This method guarantees that as many templates as possible matching the initial metadata sequence are returned,
		/// using fallback when exact matches are missing.
		/// </remarks>
		/// <param name="metadatas">The metadata array used for retrieval. Order of metadata matters.</param>
		/// <returns>
		/// An enumerable of templates matching all provided metadata, using fallback if necessary, or <see langword="null"/> if no templates are found.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="metadatas"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="metadatas"/> is empty.</exception>
		public IEnumerable<ITemplate>? TryRetrieveAllWithFallback(params IMetadata[] metadatas)
		{
			return TryRetrieve(metadatas, useFallbackSchemes: true);
		}

		/// <inheritdoc cref="TryRetrieveAllWithFallback(IMetadata[])"/>
		/// <param name="identifier">The identifier of the template to retrieve.</param>
		/// <param name="metadatas">Additional metadata to refine the template selection.</param>
		public IEnumerable<ITemplate>? TryRetrieveAllWithFallback(string identifier, params IMetadata[] metadatas)
		{
			return TryRetrieveAllWithFallback(metadatas.Prepend(new TemplateIdentifierMetadata(identifier)).ToArray());
		}

		/// <inheritdoc cref="TryRetrieveAllWithFallback(IMetadata[])"/>
		/// <param name="identifier">The identifier of the template to retrieve.</param>
		public IEnumerable<ITemplate>? TryRetrieveAllWithFallback(string identifier)
		{
			return TryRetrieveAllWithFallback(new TemplateIdentifierMetadata(identifier));
		}

		/// <summary>
		/// Retrieves all templates that best match the given metadata using fallback schemes.
		/// If an exact intersection of all provided metadata is not found, returns the last successful candidate set,
		/// potentially using fallback metadata values.
		/// </summary>
		/// <remarks>
		/// This method performs sequential matching of the provided metadata array with fallback support. <br/>
		/// Order of metadata matters. <para/>
		///
		/// Algorithm: <br/>
		/// 1. Start with the first metadata in the array. <br/>
		/// 2. If there are templates for it, keep them as the current candidate set. <br/>
		/// 3. For each subsequent metadata: <br/>
		///    - Attempt to intersect the candidate set with the templates for the current metadata. <br/>
		///    - If intersection is non-empty, continue to the next metadata. <br/>
		///    - If intersection becomes empty, return the last successful candidate set (best-effort match). <br/>
		/// 4. If a metadata key has no associated templates, the last candidate set is returned (potentially using fallback). <br/>
		/// 5. If the loop completes successfully, return the final intersection set. <para/>
		///
		/// Fallback support allows returning templates with alternate metadata when exact matches are unavailable.
		/// For example, if a template in French is requested but unavailable, an English template might be returned as a fallback. <para/>
		///
		/// Examples: <para/>
		/// Input: [ID: sample_template, LANG: fr_FR] <br/>
		/// Available templates: <br/>
		/// - [ID: sample_template, LANG: en_US] <br/>
		/// Result: <br/>
		/// - First metadata "ID: sample_template" → candidate found. <br/>
		/// - Next metadata "LANG: fr_FR" → no intersection, fallback used → returns [ID: sample_template, LANG: en_US]. <para/>
		///
		/// Input: [LANG: de_DE, ID: sample_template] <br/>
		/// Available templates: <br/>
		/// - [ID: sample_template, LANG: en_US] <br/>
		/// Result: <br/>
		/// - First metadata "LANG: de_DE" → no candidates, fallback used → returns [ID: sample_template, LANG: en_US]. <para/>
		///
		/// Input: [ID: order, TYPE: invoice, LANG: fr_FR] <br/>
		/// Available templates: <br/>
		/// - [ID: order, TYPE: invoice, LANG: en_US] <br/>
		/// - [ID: order, TYPE: receipt, LANG: fr_FR] <br/>
		/// Result: <br/>
		/// - First metadata "ID: order" → 2 candidates. <br/>
		/// - Next metadata "TYPE: invoice" → narrows to [ID: order, TYPE: invoice, LANG: en_US]. <br/>
		/// - Next metadata "LANG: fr_FR" → intersection empty, fallback used → returns [ID: order, TYPE: invoice, LANG: en_US]. <para/>
		///
		/// This approach guarantees that at least some "best-effort" templates are returned even if exact metadata matches are not available,
		/// leveraging fallback schemes when necessary.
		/// </remarks>
		/// <param name="metadatas">The metadata to use for retrieving the templates. Order matters.</param>
		/// <returns>
		/// An enumerable of templates representing the best-effort match with fallback,
		/// or <see langword="null"/> if no initial metadata matches even with fallback.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="metadatas"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="metadatas"/> is an empty array.</exception>
		public IEnumerable<ITemplate>? TryRetrieveBestAllWithFallback(params IMetadata[] metadatas)
		{
			return TryRetrieveBest(metadatas, useFallbackSchemes: true);
		}

		/// <inheritdoc cref="TryRetrieveBestAllWithFallback(IMetadata[])"/>
		/// <param name="identifier">The identifier of the template to retrieve.</param>
		/// <param name="metadatas">Additional metadata to refine the template selection.</param>
		public IEnumerable<ITemplate>? TryRetrieveBestAllWithFallback(string identifier, params IMetadata[] metadatas)
		{
			return TryRetrieveBestAllWithFallback(metadatas.Prepend(new TemplateIdentifierMetadata(identifier)).ToArray());
		}

		/// <inheritdoc cref="TryRetrieveBestAllWithFallback(IMetadata[])"/>
		/// <param name="identifier">The identifier of the template to retrieve.</param>
		public IEnumerable<ITemplate>? TryRetrieveBestAllWithFallback(string identifier)
		{
			return TryRetrieveBestAllWithFallback(new TemplateIdentifierMetadata(identifier));
		}
	}
}