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
			ICompletionProperties properties = null,
			IEnumerable<ILLModelPropertyInjector> injectors = null,
			TaskQueueParameters queueParameters = null,
			CancellationToken cancellationToken = default)
		{
			if (Capabilities != LLMCapabilities.Unknown && !Capabilities.HasFlag(LLMCapabilities.Completions))
				throw new InvalidOperationException("Chat completions are not supported by this model.");
			if (!string.IsNullOrEmpty(suffix) && Capabilities != LLMCapabilities.Unknown && !Capabilities.HasFlag(LLMCapabilities.SuffixCompletions))
				throw new InvalidOperationException("Suffix (fill-in-the-middle) completions are not supported by this model.");

			if (prompt == null)
				throw new ArgumentNullException(nameof(prompt));

			if (injectors != null)
				foreach (var injector in injectors)
					injector?.InjectCompletion(this, ref prompt, ref suffix, ref count, ref properties);

			return await TaskQueueMaster.EnqueueAsync<CompletionResult>(queueParameters, async () =>
			{
				return await Client.CreateCompletionsAsync(
					Descriptor,
					prompt,
					suffix,
					count,
					properties,
					cancellationToken);
			}, cancellationToken: cancellationToken);
		}
		
		private async Task<PartialCompletionResult> CompleteStreamingPrivateAsync(
			string prompt,
			string suffix,
			int count,
			ICompletionProperties properties = null,
			IEnumerable<ILLModelPropertyInjector> injectors = null,
			TaskQueueParameters queueParameters = null,
			CancellationToken cancellationToken = default)
		{
			if (Capabilities != LLMCapabilities.Unknown && !Capabilities.HasFlag(LLMCapabilities.Completions))
				throw new InvalidOperationException("Chat completions are not supported by this model.");
			if (!string.IsNullOrEmpty(suffix) && Capabilities != LLMCapabilities.Unknown && !Capabilities.HasFlag(LLMCapabilities.SuffixCompletions))
				throw new InvalidOperationException("Suffix (fill-in-the-middle) completions are not supported by this model.");

			if (prompt == null)
				throw new ArgumentNullException(nameof(prompt));

			if (injectors != null)
				foreach (var injector in injectors)
					injector?.InjectCompletion(this, ref prompt, ref suffix, ref count, ref properties);

			return await TaskQueueMaster.EnqueueAsync<PartialCompletionResult>(queueParameters, async () =>
			{
				return await Client.CreateStreamingCompletionsAsync(
					Descriptor,
					prompt,
					suffix,
					count,
					properties,
					cancellationToken);
			}, cancellationToken: cancellationToken);
		}

		/// <summary>
		/// Creates a completion using the provided prompt.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <returns>The completion result.</returns>
		public async Task<CompletionResult> CompleteAsync(
			string prompt)
		{
			return await CompletePrivateAsync(
				prompt,
				null,
				1,
				CompletionProperties,
				Injectors,
				QueueParameters,
				default);
		}
		
		/// <summary>
		/// Creates a completion using the provided prompt.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The completion result.</returns>
		public async Task<CompletionResult> CompleteAsync(
			string prompt,
			CancellationToken cancellationToken)
		{
			return await CompletePrivateAsync(
				prompt,
				null,
				1,
				CompletionProperties,
				Injectors,
				QueueParameters,
				cancellationToken);
		}

		/// <summary>
		/// Creates multiple completions using the provided prompt.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="count">The number of completions to generate.</param>
		/// <returns>The completion results.</returns>
		public async Task<CompletionResult> CompleteAsync(
			string prompt,
			int count)
		{
			return await CompletePrivateAsync(
				prompt,
				null,
				count,
				CompletionProperties,
				Injectors,
				QueueParameters,
				default);
		}
		
		/// <summary>
		/// Creates multiple completions using the provided prompt.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="count">The number of completions to generate.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The completion results.</returns>
		public async Task<CompletionResult> CompleteAsync(
			string prompt,
			int count,
			CancellationToken cancellationToken)
		{
			return await CompletePrivateAsync(
				prompt,
				null,
				count,
				CompletionProperties,
				Injectors,
				QueueParameters,
				cancellationToken);
		}

		/// <summary>
		/// Creates a completion with all parameters.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="properties">The custom completion properties to use for this request.</param>
		/// <param name="injectors">Additional property injectors to use for this request.</param>
		/// <param name="queueParameters">The custom queue parameters to use for this request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The completion result.</returns>
		public async Task<CompletionResult> CompleteAsync(
			string prompt,
			OptionalParameter<ICompletionProperties> properties = null,
			OptionalParameter<IEnumerable<ILLModelPropertyInjector>> injectors = null,
			OptionalParameter<TaskQueueParameters> queueParameters = null,
			CancellationToken cancellationToken = default)
		{
			return await CompletePrivateAsync(
				prompt,
				null,
				1,
				properties.GetValue(CompletionProperties),
				injectors.GetValue(Injectors),
				queueParameters.GetValue(QueueParameters),
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
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The completion results.</returns>
		public async Task<CompletionResult> CompleteAsync(
			string prompt,
			int count,
			OptionalParameter<ICompletionProperties> properties = null,
			OptionalParameter<IEnumerable<ILLModelPropertyInjector>> injectors = null,
			OptionalParameter<TaskQueueParameters> queueParameters = null,
			CancellationToken cancellationToken = default)
		{
			return await CompletePrivateAsync(
				prompt,
				null,
				count,
				properties.GetValue(CompletionProperties),
				injectors.GetValue(Injectors),
				queueParameters.GetValue(QueueParameters),
				cancellationToken);
		}

		/// <summary>
		/// Creates a suffix completion (fill-in-the-middle) using the provided prompt and suffix.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="suffix">The suffix for fill-in-the-middle completion.</param>
		/// <returns>The completion result.</returns>
		public async Task<CompletionResult> CompleteAsync(
			string prompt,
			string suffix)
		{
			return await CompletePrivateAsync(
				prompt,
				suffix,
				1,
				CompletionProperties,
				Injectors,
				QueueParameters,
				default);
		}
		
		/// <summary>
		/// Creates a suffix completion (fill-in-the-middle) using the provided prompt and suffix.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="suffix">The suffix for fill-in-the-middle completion.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The completion result.</returns>
		public async Task<CompletionResult> CompleteAsync(
			string prompt,
			string suffix,
			CancellationToken cancellationToken)
		{
			return await CompletePrivateAsync(
				prompt,
				suffix,
				1,
				CompletionProperties,
				Injectors,
				QueueParameters,
				cancellationToken);
		}

		/// <summary>
		/// Creates multiple suffix completions (fill-in-the-middle) using the provided prompt and suffix.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="suffix">The suffix for fill-in-the-middle completion.</param>
		/// <param name="count">The number of completions to generate.</param>
		/// <returns>The completion results.</returns>
		public async Task<CompletionResult> CompleteAsync(
			string prompt,
			string suffix,
			int count)
		{
			return await CompletePrivateAsync(
				prompt,
				suffix,
				count,
				CompletionProperties,
				Injectors,
				QueueParameters,
				default);
		}
		
		/// <summary>
		/// Creates multiple suffix completions (fill-in-the-middle) using the provided prompt and suffix.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="suffix">The suffix for fill-in-the-middle completion.</param>
		/// <param name="count">The number of completions to generate.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The completion results.</returns>
		public async Task<CompletionResult> CompleteAsync(
			string prompt,
			string suffix,
			int count,
			CancellationToken cancellationToken)
		{
			return await CompletePrivateAsync(
				prompt,
				suffix,
				count,
				CompletionProperties,
				Injectors,
				QueueParameters,
				cancellationToken);
		}

		/// <summary>
		/// Creates a suffix completion (fill-in-the-middle) with all parameters.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="suffix">The suffix for fill-in-the-middle completion.</param>
		/// <param name="properties">The custom completion properties to use for this request.</param>
		/// <param name="injectors">Additional property injectors to use for this request.</param>
		/// <param name="queueParameters">The custom queue parameters to use for this request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The completion result.</returns>
		public async Task<CompletionResult> CompleteAsync(
			string prompt,
			string suffix,
			OptionalParameter<ICompletionProperties> properties = null,
			OptionalParameter<IEnumerable<ILLModelPropertyInjector>> injectors = null,
			OptionalParameter<TaskQueueParameters> queueParameters = null,
			CancellationToken cancellationToken = default)
		{
			return await CompletePrivateAsync(
				prompt,
				suffix,
				1,
				properties.GetValue(CompletionProperties),
				injectors.GetValue(Injectors),
				queueParameters.GetValue(QueueParameters),
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
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The completion results.</returns>
		public async Task<CompletionResult> CompleteAsync(
			string prompt,
			string suffix,
			int count,
			OptionalParameter<ICompletionProperties> properties = null,
			OptionalParameter<IEnumerable<ILLModelPropertyInjector>> injectors = null,
			OptionalParameter<TaskQueueParameters> queueParameters = null,
			CancellationToken cancellationToken = default)
		{
			return await CompletePrivateAsync(
				prompt,
				suffix,
				count,
				properties.GetValue(CompletionProperties),
				injectors.GetValue(Injectors),
				queueParameters.GetValue(QueueParameters),
				cancellationToken);
		}

		/// <summary>
		/// Creates a streaming completion using the provided prompt.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <returns>The streaming partial completion result.</returns>
		public async Task<PartialCompletionResult> CompleteStreamingAsync(
			string prompt)
		{
			return await CompleteStreamingPrivateAsync(
				prompt,
				null,
				1,
				CompletionProperties,
				Injectors,
				QueueParameters,
				default);
		}
		
		/// <summary>
		/// Creates a streaming completion using the provided prompt.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The streaming partial completion result.</returns>
		public async Task<PartialCompletionResult> CompleteStreamingAsync(
			string prompt,
			CancellationToken cancellationToken)
		{
			return await CompleteStreamingPrivateAsync(
				prompt,
				null,
				1,
				CompletionProperties,
				Injectors,
				QueueParameters,
				cancellationToken);
		}

		/// <summary>
		/// Creates multiple streaming completions using the provided prompt.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="count">The number of completions to generate.</param>
		/// <returns>The streaming partial completion results.</returns>
		public async Task<PartialCompletionResult> CompleteStreamingAsync(
			string prompt,
			int count)
		{
			return await CompleteStreamingPrivateAsync(
				prompt,
				null,
				count,
				CompletionProperties,
				Injectors,
				QueueParameters,
				default);
		}
		
		/// <summary>
		/// Creates multiple streaming completions using the provided prompt.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="count">The number of completions to generate.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The streaming partial completion results.</returns>
		public async Task<PartialCompletionResult> CompleteStreamingAsync(
			string prompt,
			int count,
			CancellationToken cancellationToken)
		{
			return await CompleteStreamingPrivateAsync(
				prompt,
				null,
				count,
				CompletionProperties,
				Injectors,
				QueueParameters,
				cancellationToken);
		}

		/// <summary>
		/// Creates a streaming completion with all parameters.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="properties">The custom completion properties to use for this request.</param>
		/// <param name="injectors">Additional property injectors to use for this request.</param>
		/// <param name="queueParameters">The custom queue parameters to use for this request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The streaming partial completion result.</returns>
		public async Task<PartialCompletionResult> CompleteStreamingAsync(
			string prompt,
			OptionalParameter<ICompletionProperties> properties = null,
			OptionalParameter<IEnumerable<ILLModelPropertyInjector>> injectors = null,
			OptionalParameter<TaskQueueParameters> queueParameters = null,
			CancellationToken cancellationToken = default)
		{
			return await CompleteStreamingPrivateAsync(
				prompt,
				null,
				1,
				properties.GetValue(CompletionProperties),
				injectors.GetValue(Injectors),
				queueParameters.GetValue(QueueParameters),
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
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The streaming partial completion results.</returns>
		public async Task<PartialCompletionResult> CompleteStreamingAsync(
			string prompt,
			int count,
			OptionalParameter<ICompletionProperties> properties = null,
			OptionalParameter<IEnumerable<ILLModelPropertyInjector>> injectors = null,
			OptionalParameter<TaskQueueParameters> queueParameters = null,
			CancellationToken cancellationToken = default)
		{
			return await CompleteStreamingPrivateAsync(
				prompt,
				null,
				count,
				properties.GetValue(CompletionProperties),
				injectors.GetValue(Injectors),
				queueParameters.GetValue(QueueParameters),
				cancellationToken);
		}

		/// <summary>
		/// Creates a streaming suffix completion (fill-in-the-middle) using the provided prompt and suffix.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="suffix">The suffix for fill-in-the-middle completion.</param>
		/// <returns>The streaming partial completion result.</returns>
		public async Task<PartialCompletionResult> CompleteStreamingAsync(
			string prompt,
			string suffix)
		{
			return await CompleteStreamingPrivateAsync(
				prompt,
				suffix,
				1,
				CompletionProperties,
				Injectors,
				QueueParameters,
				default);
		}
		
		/// <summary>
		/// Creates a streaming suffix completion (fill-in-the-middle) using the provided prompt and suffix.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="suffix">The suffix for fill-in-the-middle completion.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The streaming partial completion result.</returns>
		public async Task<PartialCompletionResult> CompleteStreamingAsync(
			string prompt,
			string suffix,
			CancellationToken cancellationToken)
		{
			return await CompleteStreamingPrivateAsync(
				prompt,
				suffix,
				1,
				CompletionProperties,
				Injectors,
				QueueParameters,
				cancellationToken);
		}

		/// <summary>
		/// Creates multiple streaming suffix completions (fill-in-the-middle) using the provided prompt and suffix.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="suffix">The suffix for fill-in-the-middle completion.</param>
		/// <param name="count">The number of completions to generate.</param>
		/// <returns>The streaming partial completion results.</returns>
		public async Task<PartialCompletionResult> CompleteStreamingAsync(
			string prompt,
			string suffix,
			int count)
		{
			return await CompleteStreamingPrivateAsync(
				prompt,
				suffix,
				count,
				CompletionProperties,
				Injectors,
				QueueParameters,
				default);
		}
		
		/// <summary>
		/// Creates multiple streaming suffix completions (fill-in-the-middle) using the provided prompt and suffix.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="suffix">The suffix for fill-in-the-middle completion.</param>
		/// <param name="count">The number of completions to generate.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The streaming partial completion results.</returns>
		public async Task<PartialCompletionResult> CompleteStreamingAsync(
			string prompt,
			string suffix,
			int count,
			CancellationToken cancellationToken)
		{
			return await CompleteStreamingPrivateAsync(
				prompt,
				suffix,
				count,
				CompletionProperties,
				Injectors,
				QueueParameters,
				cancellationToken);
		}

		/// <summary>
		/// Creates a streaming suffix completion (fill-in-the-middle) with all parameters.
		/// </summary>
		/// <param name="prompt">The prompt to send to the model.</param>
		/// <param name="suffix">The suffix for fill-in-the-middle completion.</param>
		/// <param name="properties">The custom completion properties to use for this request.</param>
		/// <param name="injectors">Additional property injectors to use for this request.</param>
		/// <param name="queueParameters">The custom queue parameters to use for this request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The streaming partial completion result.</returns>
		public async Task<PartialCompletionResult> CompleteStreamingAsync(
			string prompt,
			string suffix,
			OptionalParameter<ICompletionProperties> properties = null,
			OptionalParameter<IEnumerable<ILLModelPropertyInjector>> injectors = null,
			OptionalParameter<TaskQueueParameters> queueParameters = null,
			CancellationToken cancellationToken = default)
		{
			return await CompleteStreamingPrivateAsync(
				prompt,
				suffix,
				1,
				properties.GetValue(CompletionProperties),
				injectors.GetValue(Injectors),
				queueParameters.GetValue(QueueParameters),
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
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The streaming partial completion results.</returns>
		public async Task<PartialCompletionResult> CompleteStreamingAsync(
			string prompt,
			string suffix,
			int count,
			OptionalParameter<ICompletionProperties> properties = null,
			OptionalParameter<IEnumerable<ILLModelPropertyInjector>> injectors = null,
			OptionalParameter<TaskQueueParameters> queueParameters = null,
			CancellationToken cancellationToken = default)
		{
			return await CompleteStreamingPrivateAsync(
				prompt,
				suffix,
				count,
				properties.GetValue(CompletionProperties),
				injectors.GetValue(Injectors),
				queueParameters.GetValue(QueueParameters),
				cancellationToken);
		}
	}
}