using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Embeddings;
using RCLargeLanguageModels.PropertyInjectors;
using RCLargeLanguageModels.Tasks;

namespace RCLargeLanguageModels
{
	public partial class LLModel
	{
		private async Task<EmbeddingResult> EmbedPrivateAsync(
			IEnumerable<string> inputs,
			IEnumerable<CompletionProperty>? properties,
			IEnumerable<ILLModelPropertyInjector>? injectors,
			TaskQueueParameters? queueParameters,
			bool validateCapabilities,
			CancellationToken cancellationToken)
		{
			if (validateCapabilities)
			{
				if (Capabilities != LLMCapabilities.Unknown && !Capabilities.HasFlag(LLMCapabilities.Embeddings))
					throw new InvalidOperationException("Embeddings are not supported by this model.");
			}

			if (inputs == null)
				throw new ArgumentNullException(nameof(inputs));
			if (!inputs.Any())
				throw new ArgumentException("Inputs cannot be empty.", nameof(inputs));

			if (injectors != null)
				foreach (var injector in injectors)
					injector?.InjectEmbedding(this, ref inputs, ref properties);

			return await TaskQueueMaster.EnqueueAsync<EmbeddingResult>(queueParameters ?? QueueParameters, async () =>
			{
				return await Client.CreateEmbeddingsAsync(
					Descriptor,
					inputs,
					properties,
					validateCapabilities,
					cancellationToken);
			}, cancellationToken: cancellationToken);
		}

		/// <summary>
		/// Creates an embedding for the specified text.
		/// </summary>
		/// <param name="input">The text to embed.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The <see cref="EmbeddingResult"/> containing the generated embedding.</returns>
		public Task<EmbeddingResult> EmbedAsync(
			string input,
			CancellationToken cancellationToken = default)
		{
			return EmbedPrivateAsync(
				new[] { input },
				CompletionProperties,
				Injectors,
				QueueParameters,
				false,
				cancellationToken);
		}

		/// <summary>
		/// Creates embeddings for the specified texts.
		/// </summary>
		/// <param name="inputs">The texts to embed.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The <see cref="EmbeddingResult"/> containing the generated embeddings.</returns>
		public Task<EmbeddingResult> EmbedAsync(
			IEnumerable<string> inputs,
			CancellationToken cancellationToken = default)
		{
			return EmbedPrivateAsync(
				inputs,
				CompletionProperties,
				Injectors,
				QueueParameters,
				false,
				cancellationToken);
		}

		/// <summary>
		/// Creates embeddings with all parameters.
		/// </summary>
		/// <param name="inputs">The texts to embed.</param>
		/// <param name="properties">The custom embedding properties to use for this request.</param>
		/// <param name="injectors">Additional property injectors to use for this request.</param>
		/// <param name="queueParameters">The custom queue parameters to use for this request.</param>
		/// <param name="validateCapabilities">Whether to validate the model's and client's capabilities before making this request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The <see cref="EmbeddingResult"/> containing the generated embeddings.</returns>
		public Task<EmbeddingResult> EmbedAsync(
			IEnumerable<string> inputs,
			IEnumerable<CompletionProperty>? properties = null,
			IEnumerable<ILLModelPropertyInjector>? injectors = null,
			TaskQueueParameters? queueParameters = null,
			bool validateCapabilities = false,
			CancellationToken cancellationToken = default)
		{
			return EmbedPrivateAsync(
				inputs,
				properties ?? CompletionProperties,
				injectors ?? Injectors,
				queueParameters ?? QueueParameters,
				validateCapabilities,
				cancellationToken);
		}

		/// <summary>
		/// Creates an embedding with all parameters.
		/// </summary>
		/// <param name="input">The text to embed.</param>
		/// <param name="properties">The custom embedding properties to use for this request.</param>
		/// <param name="injectors">Additional property injectors to use for this request.</param>
		/// <param name="queueParameters">The custom queue parameters to use for this request.</param>
		/// <param name="validateCapabilities">Whether to validate the model's and client's capabilities before making this request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The <see cref="EmbeddingResult"/> containing the generated embedding.</returns>
		public Task<EmbeddingResult> EmbedAsync(
			string input,
			IEnumerable<CompletionProperty>? properties = null,
			IEnumerable<ILLModelPropertyInjector>? injectors = null,
			TaskQueueParameters? queueParameters = null,
			bool validateCapabilities = false,
			CancellationToken cancellationToken = default)
		{
			return EmbedPrivateAsync(
				new[] { input },
				properties ?? CompletionProperties,
				injectors ?? Injectors,
				queueParameters ?? QueueParameters,
				validateCapabilities,
				cancellationToken);
		}
	}
}