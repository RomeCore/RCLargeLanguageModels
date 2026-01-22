using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Completions;

namespace RCLargeLanguageModels
{
	public partial class LLMClient
	{
		/// <summary>
		/// Creates general completions using the specified model, prompt, suffix, count and completion properties.
		/// </summary>
		/// <remarks>
		/// All properties are guaranteed to be valid to model and client compabilities and not <see langword="null"/>.
		/// </remarks>
		/// <param name="model">
		/// The model to use for the general completion.
		/// </param>
		/// <param name="prompt">
		/// The prompt to complete.
		/// </param>
		/// <param name="suffix">
		/// The suffix to use in fill-in-the-middle completions.
		/// </param>
		/// <param name="count">
		/// Count of completions to create.
		/// </param>
		/// <param name="properties">
		/// The properties that affects the result message.
		/// </param>
		/// <param name="cancellationToken">
		/// The cancellation token used to cancel the completion generation.
		/// </param>
		/// <returns>The array of <see cref="Completion"/> that contains general completions results.</returns>
		protected abstract Task<CompletionResult> CreateCompletionsOverrideAsync(
			LLModelDescriptor model,
			string prompt,
			string? suffix,
			int count,
			IEnumerable<CompletionProperty> properties,
			CancellationToken cancellationToken);

		/// <inheritdoc cref="CreateCompletionsOverrideAsync"/>
		/// <summary>
		/// Creates streaming general completions using the specified model, prompt, suffix, count and completion properties.
		/// </summary>
		/// <returns>The array of <see cref="PartialCompletion"/> that contains streaming general completions results.</returns>
		protected abstract Task<PartialCompletionResult> CreateStreamingCompletionsOverrideAsync(
			LLModelDescriptor model,
			string prompt,
			string? suffix,
			int count,
			IEnumerable<CompletionProperty> properties,
			CancellationToken cancellationToken);

		/// <summary>
		/// Validates completion parameters before using them in completions.
		/// </summary>
		/// <param name="model"></param>
		/// <param name="prompt"></param>
		/// <param name="suffix"></param>
		/// <param name="streaming"></param>
		/// <param name="count"></param>
		/// <param name="properties"></param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="LLMException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		protected virtual void ValidateCompletionParameters(
			LLModelDescriptor model,
			string prompt,
			string? suffix,
			bool streaming,
			int count,
			ref IEnumerable<CompletionProperty> properties)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			var selfCaps = Capabilities;
			bool selfCapsKnown = !selfCaps.IsUnknown();

			if (selfCapsKnown && !selfCaps.HasFlag(LLMCapabilities.Completions))
				throw new LLMException("Client does not support general completions.", this);

			var caps = model.Capabilities;
			bool capsKnown = !caps.IsUnknown();

			if (capsKnown && !caps.HasFlag(LLMCapabilities.Completions))
				throw new LLMException("Model does not support general completions.", model);

			if (streaming)
			{
				if (selfCapsKnown && !selfCaps.HasFlag(LLMCapabilities.StreamingCompletions))
					throw new LLMException("Client does not support streaming completions.", this);
				if (capsKnown && !caps.HasFlag(LLMCapabilities.StreamingCompletions))
					throw new LLMException("Model does not support streaming completions.", model);
			}

			if (prompt == null)
				throw new ArgumentNullException(prompt);

			if (suffix != null)
			{
				if (selfCapsKnown && !selfCaps.HasFlag(LLMCapabilities.SuffixCompletions))
					throw new LLMException($"Client does not support fill-in-the-middle completions.", this);
				if (capsKnown && !caps.HasFlag(LLMCapabilities.SuffixCompletions))
					throw new LLMException($"Model does not support fill-in-the-middle completions.", model);
			}

			if (count < 1)
				throw new ArgumentOutOfRangeException(nameof(count), "Count of completions must be at least 1.");

			if (count > 1)
			{
				if (selfCapsKnown && !selfCaps.HasFlag(LLMCapabilities.MultipleCompletions))
					throw new LLMException($"Client does not support multiple completions per one request. (count:{count} > 1)", this);
				if (capsKnown && !caps.HasFlag(LLMCapabilities.MultipleCompletions))
					throw new LLMException($"Model does not support multiple completions per one request. (count:{count} > 1)", model);
			}

			properties ??= Enumerable.Empty<CompletionProperty>();
		}

		/// <summary>
		/// Creates a general completion using the specified model, prompt, suffix and completion properties.
		/// </summary>
		/// <param name="model">The model to use for the chat completion.</param>
		/// <param name="prompt">The prompt to complete.</param>
		/// <param name="suffix">The suffix to use in fill-in-the-middle completions.</param>
		/// <param name="properties">The properties that affects the result completion. Can be <see langword="null"/> to use the default properties.</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>The <see cref="CompletionResult"/> that contains the result of completion.</returns>
		public async Task<CompletionResult> CreateCompletionAsync(
			LLModelDescriptor model,
			string prompt,
			string? suffix,
			IEnumerable<CompletionProperty> properties = null,
			CancellationToken cancellationToken = default)
		{
			ValidateCompletionParameters(model, prompt, suffix, false, 1, ref properties);

			return await CreateCompletionsOverrideAsync(
				model,
				prompt,
				suffix,
				1,
				properties,
				cancellationToken);
		}

		/// <summary>
		/// Creates a streaming general completion using the specified model, prompt, suffix and completion properties.
		/// </summary>
		/// <param name="model">The model to use for the chat completion.</param>
		/// <param name="prompt">The prompt to complete.</param>
		/// <param name="suffix">The suffix to use in fill-in-the-middle completions.</param>
		/// <param name="properties">The properties that affects the result completion. Can be <see langword="null"/> to use the default properties.</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>The <see cref="PartialCompletionResult"/> that contains the streaming result of completion.</returns>
		public async Task<PartialCompletionResult> CreateStreamingCompletionAsync(
			LLModelDescriptor model,
			string prompt,
			string? suffix,
			IEnumerable<CompletionProperty> properties = null,
			CancellationToken cancellationToken = default)
		{
			ValidateCompletionParameters(model, prompt, suffix, true, 1, ref properties);

			return await CreateStreamingCompletionsOverrideAsync(
				model,
				prompt,
				suffix,
				1,
				properties,
				cancellationToken);
		}

		/// <summary>
		/// Creates general completions using the specified model, prompt, suffix, count and completion properties.
		/// </summary>
		/// <param name="model">The model to use for the chat completion.</param>
		/// <param name="prompt">The prompt to complete.</param>
		/// <param name="suffix">The suffix to use in fill-in-the-middle completions.</param>
		/// <param name="count">The count of completions to create. Must be at least 1.</param>
		/// <param name="properties">The properties that affects the result completion. Can be <see langword="null"/> to use the default properties.</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>The <see cref="Completion"/> that contains the results of completions.</returns>
		public async Task<CompletionResult> CreateCompletionsAsync(
			LLModelDescriptor model,
			string prompt,
			string? suffix,
			int count,
			IEnumerable<CompletionProperty> properties = null,
			CancellationToken cancellationToken = default)
		{
			ValidateCompletionParameters(model, prompt, suffix, false, count, ref properties);

			return await CreateCompletionsOverrideAsync(
				model,
				prompt,
				suffix,
				count,
				properties,
				cancellationToken);
		}

		/// <summary>
		/// Creates multiple streaming general completions using the specified model, prompt, suffix, count and completion properties.
		/// </summary>
		/// <param name="model">The model to use for the chat completion.</param>
		/// <param name="prompt">The prompt to complete.</param>
		/// <param name="suffix">The suffix to use in fill-in-the-middle completions.</param>
		/// <param name="count">The count of completions to create. Must be at least 1.</param>
		/// <param name="properties">The properties that affects the result completion. Can be <see langword="null"/> to use the default properties.</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>The <see cref="PartialCompletionResult"/> that contains the streaming results of completions.</returns>
		public async Task<PartialCompletionResult> CreateStreamingCompletionsAsync(
			LLModelDescriptor model,
			string prompt,
			string? suffix,
			int count,
			IEnumerable<CompletionProperty> properties = null,
			CancellationToken cancellationToken = default)
		{
			ValidateCompletionParameters(model, prompt, suffix, true, count, ref properties);

			return await CreateStreamingCompletionsOverrideAsync(
				model,
				prompt,
				suffix,
				count,
				properties,
				cancellationToken);
		}

		/// <summary>
		/// Creates a general completion using the specified model, prompt and completion properties.
		/// </summary>
		/// <param name="model">The model to use for the chat completion.</param>
		/// <param name="prompt">The prompt to complete.</param>
		/// <param name="properties">The properties that affects the result completion. Can be <see langword="null"/> to use the default properties.</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>The <see cref="CompletionResult"/> that contains the result of completion.</returns>
		public async Task<CompletionResult> CreateCompletionAsync(
			LLModelDescriptor model,
			string prompt,
			IEnumerable<CompletionProperty> properties = null,
			CancellationToken cancellationToken = default)
		{
			ValidateCompletionParameters(model, prompt, null, false, 1, ref properties);

			return await CreateCompletionsOverrideAsync(
				model,
				prompt,
				null,
				1,
				properties,
				cancellationToken);
		}

		/// <summary>
		/// Creates a streaming general completion using the specified model, prompt and completion properties.
		/// </summary>
		/// <param name="model">The model to use for the chat completion.</param>
		/// <param name="prompt">The prompt to complete.</param>
		/// <param name="properties">The properties that affects the result completion. Can be <see langword="null"/> to use the default properties.</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>The <see cref="PartialCompletionResult"/> that contains the streaming result of completion.</returns>
		public async Task<PartialCompletionResult> CreateStreamingCompletionAsync(
			LLModelDescriptor model,
			string prompt,
			IEnumerable<CompletionProperty> properties = null,
			CancellationToken cancellationToken = default)
		{
			ValidateCompletionParameters(model, prompt, null, true, 1, ref properties);

			return await CreateStreamingCompletionsOverrideAsync(
				model,
				prompt,
				null,
				1,
				properties,
				cancellationToken);
		}

		/// <summary>
		/// Creates general completions using the specified model, prompt, count and completion properties.
		/// </summary>
		/// <param name="model">The model to use for the chat completion.</param>
		/// <param name="prompt">The prompt to complete.</param>
		/// <param name="count">The count of completions to create. Must be at least 1.</param>
		/// <param name="properties">The properties that affects the result completion. Can be <see langword="null"/> to use the default properties.</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>The <see cref="CompletionResult"/> that contains the results of completions.</returns>
		public async Task<CompletionResult> CreateCompletionsAsync(
			LLModelDescriptor model,
			string prompt,
			int count,
			IEnumerable<CompletionProperty> properties = null,
			CancellationToken cancellationToken = default)
		{
			ValidateCompletionParameters(model, prompt, null, false, count, ref properties);

			return await CreateCompletionsOverrideAsync(
				model,
				prompt,
				null,
				count,
				properties,
				cancellationToken);
		}

		/// <summary>
		/// Creates multiple streaming general completions using the specified model, prompt, count and completion properties.
		/// </summary>
		/// <param name="model">The model to use for the chat completion.</param>
		/// <param name="prompt">The prompt to complete.</param>
		/// <param name="count">The count of completions to create. Must be at least 1.</param>
		/// <param name="properties">The properties that affects the result completion. Can be <see langword="null"/> to use the default properties.</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>The <see cref="PartialCompletionResult"/> that contains the streaming results of completions.</returns>
		public async Task<PartialCompletionResult> CreateStreamingCompletionsAsync(
			LLModelDescriptor model,
			string prompt,
			int count,
			IEnumerable<CompletionProperty> properties = null,
			CancellationToken cancellationToken = default)
		{
			ValidateCompletionParameters(model, prompt, null, true, count, ref properties);

			return await CreateStreamingCompletionsOverrideAsync(
				model,
				prompt,
				null,
				count,
				properties,
				cancellationToken);
		}
	}
}