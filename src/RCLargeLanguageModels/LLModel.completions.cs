using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.PropertyInjectors;
using RCLargeLanguageModels.Tasks;

namespace RCLargeLanguageModels
{
	public partial class LLModel
	{
		private async Task<CompletionResult> CompletePrivateAsync(
			string prompt,
			string suffix,
			int count,
			IEnumerable<CompletionProperty> properties,
			IEnumerable<ILLModelPropertyInjector> injectors,
			TaskQueueParameters queueParameters,
			bool validateCapabilities,
			CancellationToken cancellationToken)
		{
			if (validateCapabilities)
			{
				if (Capabilities != LLMCapabilities.Unknown && !Capabilities.HasFlag(LLMCapabilities.Completions))
					throw new InvalidOperationException("General completions are not supported by this model.");
				if (!string.IsNullOrEmpty(suffix) && Capabilities != LLMCapabilities.Unknown && !Capabilities.HasFlag(LLMCapabilities.SuffixCompletions))
					throw new InvalidOperationException("Suffix (fill-in-the-middle) completions are not supported by this model.");
			}

			if (prompt == null)
				throw new ArgumentNullException(nameof(prompt));

			var injectorsList = injectors != null ? (injectors as IReadOnlyList<ILLModelPropertyInjector> ?? new List<ILLModelPropertyInjector>(injectors)) : null;
			if (injectors != null && injectorsList.Count > 0)
			{
				var propertiesList = properties as List<CompletionProperty> ?? new List<CompletionProperty>(properties);

				var injectionParameters = new CompletionInjectionParameters(this, prompt, suffix, count, propertiesList);
				foreach (var injector in injectorsList)
					await injector.InjectCompletionAsync(injectionParameters);

				prompt = injectionParameters.Prompt;
				suffix = injectionParameters.Suffix;
				count = injectionParameters.Count;
				properties = injectionParameters.Properties;
			}

			return await TaskQueueMaster.EnqueueAsync<CompletionResult>(queueParameters, async () =>
			{
				return await Client.CreateCompletionsAsync(
					Descriptor,
					prompt,
					suffix,
					count,
					properties,
					validateCapabilities,
					cancellationToken);
			}, cancellationToken: cancellationToken);
		}
		
		private async Task<PartialCompletionResult> CompleteStreamingPrivateAsync(
			string prompt,
			string suffix,
			int count,
			IEnumerable<CompletionProperty> properties,
			IEnumerable<ILLModelPropertyInjector> injectors,
			TaskQueueParameters queueParameters,
			bool validateCapabilities,
			CancellationToken cancellationToken)
		{
			if (validateCapabilities)
			{
				if (Capabilities != LLMCapabilities.Unknown && !Capabilities.HasFlag(LLMCapabilities.Completions))
					throw new InvalidOperationException("General completions are not supported by this model.");
				if (!string.IsNullOrEmpty(suffix) && Capabilities != LLMCapabilities.Unknown && !Capabilities.HasFlag(LLMCapabilities.SuffixCompletions))
					throw new InvalidOperationException("Suffix (fill-in-the-middle) completions are not supported by this model.");
			}

			if (prompt == null)
				throw new ArgumentNullException(nameof(prompt));

			var injectorsList = injectors != null ? (injectors as IReadOnlyList<ILLModelPropertyInjector> ?? new List<ILLModelPropertyInjector>(injectors)) : null;
			if (injectors != null && injectorsList.Count > 0)
			{
				var propertiesList = properties as List<CompletionProperty> ?? new List<CompletionProperty>(properties);

				var injectionParameters = new CompletionInjectionParameters(this, prompt, suffix, count, propertiesList);
				foreach (var injector in injectorsList)
					await injector.InjectCompletionAsync(injectionParameters);

				prompt = injectionParameters.Prompt;
				suffix = injectionParameters.Suffix;
				count = injectionParameters.Count;
				properties = injectionParameters.Properties;
			}

			return await TaskQueueMaster.EnqueueAsync<PartialCompletionResult>(queueParameters, async () =>
			{
				return await Client.CreateStreamingCompletionsAsync(
					Descriptor,
					prompt,
					suffix,
					count,
					properties,
					validateCapabilities,
					cancellationToken);
			}, cancellationToken: cancellationToken);
		}

		/// <summary>
		/// Creates multiple completions with all parameters.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="properties">The custom completion properties to use for this request.</param>
		/// <param name="injectors">Additional property injectors to use for this request.</param>
		/// <param name="queueParameters">The custom queue parameters to use for this request.</param>
		/// <param name="validateCapabilities">Whether to validate the model's and client's capabilities before making this request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The completion results.</returns>
		public async Task<CompletionResult> CompleteAsync(
			string prompt,
			IEnumerable<CompletionProperty> properties = null,
			IEnumerable<ILLModelPropertyInjector> injectors = null,
			TaskQueueParameters queueParameters = null,
			bool validateCapabilities = false,
			CancellationToken cancellationToken = default)
		{
			return await CompletePrivateAsync(
				prompt,
				null,
				1,
				properties ?? CompletionProperties,
				injectors ?? Injectors,
				queueParameters ?? QueueParameters,
				validateCapabilities,
				cancellationToken);
		}

		/// <summary>
		/// Creates multiple completions with all parameters.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="count">The number of completions to generate.</param>
		/// <param name="properties">The custom completion properties to use for this request.</param>
		/// <param name="injectors">Additional property injectors to use for this request.</param>
		/// <param name="queueParameters">The custom queue parameters to use for this request.</param>
		/// <param name="validateCapabilities">Whether to validate the model's and client's capabilities before making this request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The completion results.</returns>
		public async Task<CompletionResult> CompleteAsync(
			string prompt,
			int count,
			IEnumerable<CompletionProperty> properties = null,
			IEnumerable<ILLModelPropertyInjector> injectors = null,
			TaskQueueParameters queueParameters = null,
			bool validateCapabilities = false,
			CancellationToken cancellationToken = default)
		{
			return await CompletePrivateAsync(
				prompt,
				null,
				count,
				properties ?? CompletionProperties,
				injectors ?? Injectors,
				queueParameters ?? QueueParameters,
				validateCapabilities,
				cancellationToken);
		}

		/// <summary>
		/// Creates multiple suffix completions (fill-in-the-middle) with all parameters.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="suffix">The suffix for fill-in-the-middle completion.</param>
		/// <param name="properties">The custom completion properties to use for this request.</param>
		/// <param name="injectors">Additional property injectors to use for this request.</param>
		/// <param name="queueParameters">The custom queue parameters to use for this request.</param>
		/// <param name="validateCapabilities">Whether to validate the model's and client's capabilities before making this request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The completion results.</returns>
		public async Task<CompletionResult> CompleteAsync(
			string prompt,
			string suffix,
			IEnumerable<CompletionProperty> properties = null,
			IEnumerable<ILLModelPropertyInjector> injectors = null,
			TaskQueueParameters queueParameters = null,
			bool validateCapabilities = false,
			CancellationToken cancellationToken = default)
		{
			return await CompletePrivateAsync(
				prompt,
				suffix,
				1,
				properties ?? CompletionProperties,
				injectors ?? Injectors,
				queueParameters ?? QueueParameters,
				validateCapabilities,
				cancellationToken);
		}

		/// <summary>
		/// Creates multiple suffix completions (fill-in-the-middle) with all parameters.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="suffix">The suffix for fill-in-the-middle completion.</param>
		/// <param name="count">The number of completions to generate.</param>
		/// <param name="properties">The custom completion properties to use for this request.</param>
		/// <param name="injectors">Additional property injectors to use for this request.</param>
		/// <param name="queueParameters">The custom queue parameters to use for this request.</param>
		/// <param name="validateCapabilities">Whether to validate the model's and client's capabilities before making this request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The completion results.</returns>
		public async Task<CompletionResult> CompleteAsync(
			string prompt,
			string suffix,
			int count,
			IEnumerable<CompletionProperty> properties = null,
			IEnumerable<ILLModelPropertyInjector> injectors = null,
			TaskQueueParameters queueParameters = null,
			bool validateCapabilities = false,
			CancellationToken cancellationToken = default)
		{
			return await CompletePrivateAsync(
				prompt,
				suffix,
				count,
				properties ?? CompletionProperties,
				injectors ?? Injectors,
				queueParameters ?? QueueParameters,
				validateCapabilities,
				cancellationToken);
		}

		/// <summary>
		/// Creates multiple streaming completions with all parameters.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="properties">The custom completion properties to use for this request.</param>
		/// <param name="injectors">Additional property injectors to use for this request.</param>
		/// <param name="queueParameters">The custom queue parameters to use for this request.</param>
		/// <param name="validateCapabilities">Whether to validate the model's and client's capabilities before making this request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The streaming partial completion results.</returns>
		public async Task<PartialCompletionResult> CompleteStreamingAsync(
			string prompt,
			IEnumerable<CompletionProperty> properties = null,
			IEnumerable<ILLModelPropertyInjector> injectors = null,
			TaskQueueParameters queueParameters = null,
			bool validateCapabilities = false,
			CancellationToken cancellationToken = default)
		{
			return await CompleteStreamingPrivateAsync(
				prompt,
				null,
				1,
				properties ?? CompletionProperties,
				injectors ?? Injectors,
				queueParameters ?? QueueParameters,
				validateCapabilities,
				cancellationToken);
		}

		/// <summary>
		/// Creates multiple streaming completions with all parameters.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="count">The number of completions to generate.</param>
		/// <param name="properties">The custom completion properties to use for this request.</param>
		/// <param name="injectors">Additional property injectors to use for this request.</param>
		/// <param name="queueParameters">The custom queue parameters to use for this request.</param>
		/// <param name="validateCapabilities">Whether to validate the model's and client's capabilities before making this request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The streaming partial completion results.</returns>
		public async Task<PartialCompletionResult> CompleteStreamingAsync(
			string prompt,
			int count,
			IEnumerable<CompletionProperty> properties = null,
			IEnumerable<ILLModelPropertyInjector> injectors = null,
			TaskQueueParameters queueParameters = null,
			bool validateCapabilities = false,
			CancellationToken cancellationToken = default)
		{
			return await CompleteStreamingPrivateAsync(
				prompt,
				null,
				count,
				properties ?? CompletionProperties,
				injectors ?? Injectors,
				queueParameters ?? QueueParameters,
				validateCapabilities,
				cancellationToken);
		}

		/// <summary>
		/// Creates multiple streaming suffix completions (fill-in-the-middle) with all parameters.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="suffix">The suffix for fill-in-the-middle completion.</param>
		/// <param name="properties">The custom completion properties to use for this request.</param>
		/// <param name="injectors">Additional property injectors to use for this request.</param>
		/// <param name="queueParameters">The custom queue parameters to use for this request.</param>
		/// <param name="validateCapabilities">Whether to validate the model's and client's capabilities before making this request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The streaming partial completion results.</returns>
		public async Task<PartialCompletionResult> CompleteStreamingAsync(
			string prompt,
			string suffix,
			IEnumerable<CompletionProperty> properties = null,
			IEnumerable<ILLModelPropertyInjector> injectors = null,
			TaskQueueParameters queueParameters = null,
			bool validateCapabilities = false,
			CancellationToken cancellationToken = default)
		{
			return await CompleteStreamingPrivateAsync(
				prompt,
				suffix,
				1,
				properties ?? CompletionProperties,
				injectors ?? Injectors,
				queueParameters ?? QueueParameters,
				validateCapabilities,
				cancellationToken);
		}

		/// <summary>
		/// Creates multiple streaming suffix completions (fill-in-the-middle) with all parameters.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="suffix">The suffix for fill-in-the-middle completion.</param>
		/// <param name="count">The number of completions to generate.</param>
		/// <param name="properties">The custom completion properties to use for this request.</param>
		/// <param name="injectors">Additional property injectors to use for this request.</param>
		/// <param name="queueParameters">The custom queue parameters to use for this request.</param>
		/// <param name="validateCapabilities">Whether to validate the model's and client's capabilities before making this request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The streaming partial completion results.</returns>
		public async Task<PartialCompletionResult> CompleteStreamingAsync(
			string prompt,
			string suffix,
			int count,
			IEnumerable<CompletionProperty> properties = null,
			IEnumerable<ILLModelPropertyInjector> injectors = null,
			TaskQueueParameters queueParameters = null,
			bool validateCapabilities = false,
			CancellationToken cancellationToken = default)
		{
			return await CompleteStreamingPrivateAsync(
				prompt,
				suffix,
				count,
				properties ?? CompletionProperties,
				injectors ?? Injectors,
				queueParameters ?? QueueParameters,
				validateCapabilities,
				cancellationToken);
		}
	}
}