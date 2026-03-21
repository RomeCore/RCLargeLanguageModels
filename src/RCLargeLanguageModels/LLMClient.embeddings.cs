using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Embeddings;
using RCLargeLanguageModels.Exceptions;

namespace RCLargeLanguageModels
{
	public partial class LLMClient
	{
		/// <summary>
		/// Creates embeddings using the specified model and inputs.
		/// </summary>
		/// <param name="model">The model to use for creating embeddings.</param>
		/// <param name="inputs">The input texts to embed.</param>
		/// <param name="properties">The properties that affect the embedding generation.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The <see cref="EmbeddingResult"/> containing the generated embeddings.</returns>
		protected abstract Task<EmbeddingResult> CreateEmbeddingsOverrideAsync(
			LLModelDescriptor model,
			List<string> inputs,
			List<CompletionProperty> properties,
			CancellationToken cancellationToken);

		/// <summary>
		/// Validates embedding parameters before using them to create embeddings.
		/// </summary>
		/// <param name="model">The model descriptor.</param>
		/// <param name="inputs">The input texts.</param>
		/// <param name="properties">The completion properties.</param>
		/// <param name="validateCapabilities">Whether to validate capabilities.</param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="LLMException"></exception>
		protected virtual void ValidateEmbeddingParameters(
			LLModelDescriptor model,
			ref List<string> inputs,
			ref List<CompletionProperty> properties,
			bool validateCapabilities)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			inputs = inputs ?? throw new ArgumentNullException(nameof(inputs));
			if (inputs.Count == 0)
				throw new ArgumentException("Inputs cannot be empty.", nameof(inputs));

			properties ??= new List<CompletionProperty>();

			if (validateCapabilities)
			{
				var selfCaps = Capabilities;
				bool selfCapsKnown = !selfCaps.IsUnknown();
				var caps = model.Capabilities;
				bool capsKnown = !caps.IsUnknown();

				if (selfCapsKnown && !selfCaps.HasFlag(LLMCapabilities.Embeddings))
					throw new LLMException("Client does not support embeddings.", this);

				if (capsKnown && !caps.HasFlag(LLMCapabilities.Embeddings))
					throw new LLMException("Model does not support embeddings.", model);
			}
		}

		/// <summary>
		/// Creates embeddings for the specified input text.
		/// </summary>
		/// <param name="model">The model to use for creating embeddings.</param>
		/// <param name="input">The input text to embed.</param>
		/// <param name="properties">The properties that affect the embedding generation.</param>
		/// <param name="validateCapabilities">Whether to validate capabilities.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The <see cref="EmbeddingResult"/> containing the generated embedding.</returns>
		public async Task<EmbeddingResult> CreateEmbeddingAsync(
			LLModelDescriptor model,
			string input,
			IEnumerable<CompletionProperty>? properties = null,
			bool validateCapabilities = false,
			CancellationToken cancellationToken = default)
		{
			List<string> inputs = new() { input };
			var propertiesList = properties as List<CompletionProperty> ?? new List<CompletionProperty>(properties);

			ValidateEmbeddingParameters(model, ref inputs, ref propertiesList, validateCapabilities);

			return await CreateEmbeddingsOverrideAsync(model, inputs, propertiesList, cancellationToken);
		}

		/// <summary>
		/// Creates embeddings for the specified input texts.
		/// </summary>
		/// <param name="model">The model to use for creating embeddings.</param>
		/// <param name="inputs">The input texts to embed.</param>
		/// <param name="properties">The properties that affect the embedding generation.</param>
		/// <param name="validateCapabilities">Whether to validate capabilities.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The <see cref="EmbeddingResult"/> containing the generated embeddings.</returns>
		public async Task<EmbeddingResult> CreateEmbeddingsAsync(
			LLModelDescriptor model,
			IEnumerable<string> inputs,
			IEnumerable<CompletionProperty>? properties = null,
			bool validateCapabilities = false,
			CancellationToken cancellationToken = default)
		{
			var inputsList = inputs as List<string> ?? new List<string>(inputs);
			var propertiesList = properties as List<CompletionProperty> ?? new List<CompletionProperty>(properties);

			ValidateEmbeddingParameters(model, ref inputsList, ref propertiesList, validateCapabilities);

			return await CreateEmbeddingsOverrideAsync(model, inputsList, propertiesList, cancellationToken);
		}
	}
}