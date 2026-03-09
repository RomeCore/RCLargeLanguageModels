using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Exceptions;
using RCLargeLanguageModels.Formats;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tools;

namespace RCLargeLanguageModels
{
	public partial class LLMClient
	{
		/// <summary>
		/// Creates chat completions using the specified model, messages, count, properties, native output format definition and tools.
		/// </summary>
		/// <remarks>
		/// All properties are guaranteed to be valid to model and client compabilities and not <see langword="null"/>.
		/// </remarks>
		/// <param name="model">
		/// The model to use for the chat completion.
		/// </param>
		/// <param name="messages">
		/// The messages to use for the chat completion.
		/// </param>
		/// <param name="count">
		/// Count of completions to create.
		/// </param>
		/// <param name="properties">
		/// The properties that affects the result message.
		/// </param>
		/// <param name="outputFormatDefinition">
		/// The native output format definition that should be used to natively configure the output format.
		/// </param>
		/// <param name="tools">
		/// The available tools that can be called by model.
		/// </param>
		/// <param name="cancellationToken">
		/// The cancellation token used to cancel the message generation.
		/// </param>
		/// <returns>The <see cref="ChatCompletionResult"/> that contains chat completions.</returns>
		protected abstract Task<ChatCompletionResult> CreateChatCompletionsOverrideAsync(
			LLModelDescriptor model,
			IEnumerable<IMessage> messages,
			int count,
			IEnumerable<CompletionProperty> properties,
			OutputFormatDefinition outputFormatDefinition,
			IEnumerable<ITool> tools,
			CancellationToken cancellationToken);

		/// <summary>
		/// Creates streaming chat completions using the specified model, messages, properties, native output format definition and tools.
		/// </summary>
		/// <remarks>
		/// All properties are guaranteed to be valid to model and client compabilities and not <see langword="null"/>.
		/// </remarks>
		/// <param name="model">
		/// The model to use for the chat completion.
		/// </param>
		/// <param name="messages">
		/// The messages to use for the chat completion.
		/// </param>
		/// <param name="count">
		/// Count of completions to create.
		/// </param>
		/// <param name="properties">
		/// The properties that affects the result message.
		/// </param>
		/// <param name="outputFormatDefinition">
		/// The native output format definition that should be used to natively configure the output format.
		/// </param>
		/// <param name="tools">
		/// The available tools that can be called by model.
		/// </param>
		/// <param name="cancellationToken">
		/// The cancellation token used to cancel the message generation.
		/// </param>
		/// <returns>The <see cref="PartialChatCompletionResult"/> that contains the streaming chat completions.</returns>
		protected abstract Task<PartialChatCompletionResult> CreateStreamingChatCompletionsOverrideAsync(
			LLModelDescriptor model,
			IEnumerable<IMessage> messages,
			int count,
			IEnumerable<CompletionProperty> properties,
			OutputFormatDefinition outputFormatDefinition,
			IEnumerable<ITool> tools,
			CancellationToken cancellationToken);

		/// <summary>
		/// Validates chat completion parameters before using them in chat completions.
		/// </summary>
		/// <param name="model"></param>
		/// <param name="messages"></param>
		/// <param name="streaming"></param>
		/// <param name="count"></param>
		/// <param name="properties"></param>
		/// <param name="outputFormatDefinition"></param>
		/// <param name="tools"></param>
		/// <param name="validateCapabilities"></param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="LLMException"></exception>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		protected virtual void ValidateChatCompletionParameters(
			LLModelDescriptor model,
			ref IEnumerable<IMessage> messages,
			bool streaming,
			int count,
			ref IEnumerable<CompletionProperty> properties,
			ref OutputFormatDefinition outputFormatDefinition,
			ref IEnumerable<ITool> tools,
			bool validateCapabilities)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));

			var messagesList = messages?.ToList() ?? throw new ArgumentNullException(nameof(messages));
			if (messagesList.Count == 0)
				throw new ArgumentException("Messages cannot be empty.", nameof(messages));
			messages = messagesList;

			properties ??= Enumerable.Empty<CompletionProperty>();

			var toolList = tools?.ToList() ?? Enumerable.Empty<ITool>();

			if (outputFormatDefinition == null)
				outputFormatDefinition = OutputFormatDefinition.Empty;

			if (validateCapabilities)
			{
				var selfCaps = Capabilities;
				bool selfCapsKnown = !selfCaps.IsUnknown();
				var caps = model.Capabilities;
				bool capsKnown = !caps.IsUnknown();

				if (selfCapsKnown && !selfCaps.HasFlag(LLMCapabilities.ChatCompletions))
					throw new LLMException("Client does not support chat completions.", this);

				if (capsKnown && !caps.HasFlag(LLMCapabilities.ChatCompletions))
					throw new LLMException("Model does not support chat completions.", model);

				if (streaming)
				{
					if (selfCapsKnown && !selfCaps.HasFlag(LLMCapabilities.StreamingCompletions))
						throw new LLMException("Client does not support streaming completions.", this);
					if (capsKnown && !caps.HasFlag(LLMCapabilities.StreamingCompletions))
						throw new LLMException("Model does not support streaming completions.", model);
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

				if (toolList.Any())
				{
					if (selfCapsKnown && !selfCaps.HasFlag(LLMCapabilities.ToolSupport))
						throw new LLMException("Client does not support tools.", this);
					if (capsKnown && !caps.HasFlag(LLMCapabilities.ToolSupport))
						throw new LLMException("Model does not support tools.", model);
				}

				if (outputFormatDefinition != OutputFormatDefinition.Empty)
				{
					if (!ReferenceEquals(SupportedOutputFormats, OutputFormatSupportSet.All) &&
						!SupportedOutputFormats.Supports(outputFormatDefinition.Type))
						throw new LLMException($"Output format type {outputFormatDefinition.Type} is not supported by the client.", this);
					if (!ReferenceEquals(model.SupportedOutputFormats, OutputFormatSupportSet.All) &&
						!model.SupportedOutputFormats.Supports(outputFormatDefinition.Type))
						throw new LLMException($"Output format type {outputFormatDefinition.Type} is not supported by the model.", model);
				}
			}

			tools = toolList;
		}

		/// <summary>
		/// Creates a chat completion using the specified model, messages, properties, native output format definition and tools.
		/// </summary>
		/// <param name="model">The model to use for the chat completion. Must be non-<see langword="null"/>.</param>
		/// <param name="messages">The messages to use for the chat completion. Must be non-empty.</param>
		/// <param name="properties">The properties that affects the result message. Can be <see langword="null"/> to use the default properties.</param>
		/// <param name="outputFormatDefinition">
		/// The native output format definition that should be used to natively configure the output format.
		/// Can be <see langword="null"/> to use the empty output format definition.
		/// </param>
		/// <param name="tools">The available tools that can be called by model. Can be <see langword="null"/> to provide no tools.</param>
		/// <param name="validateCapabilities">Whether to validate the capabilities of the client and model before creating the completions. Default is <see langword="false"/>.</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>The <see cref="ChatCompletionResult"/> that contains the chat completion.</returns>
		public async Task<ChatCompletionResult> CreateChatCompletionAsync(
			LLModelDescriptor model,
			IEnumerable<IMessage> messages,
			IEnumerable<CompletionProperty>? properties = null,
			OutputFormatDefinition? outputFormatDefinition = null,
			IEnumerable<ITool>? tools = null,
			bool validateCapabilities = false,
			CancellationToken cancellationToken = default)
		{
			ValidateChatCompletionParameters(model,
				ref messages,
				false,
				1,
				ref properties,
				ref outputFormatDefinition,
				ref tools,
				validateCapabilities);

			return await CreateChatCompletionsOverrideAsync(
				model,
				messages,
				1,
				properties,
				outputFormatDefinition,
				tools,
				cancellationToken);
		}

		/// <summary>
		/// Creates a streaming chat completion using the specified model, messages, properties, native output format definition and tools.
		/// </summary>
		/// <param name="model">The model to use for the chat completion. Must be non-<see langword="null"/>.</param>
		/// <param name="messages">The messages to use for the chat completion. Must be non-empty.</param>
		/// <param name="properties">The properties that affects the result message. Can be <see langword="null"/> to use the default properties.</param>
		/// <param name="outputFormatDefinition">
		/// The native output format definition that should be used to natively configure the output format.
		/// Can be <see langword="null"/> to use the empty output format definition.
		/// </param>
		/// <param name="tools">The available tools that can be called by model. Can be <see langword="null"/> to provide no tools.</param>
		/// <param name="validateCapabilities">Whether to validate the capabilities of the client and model before creating the completions. Default is <see langword="false"/>.</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>The <see cref="PartialAssistantMessage"/> that contains the streaming chat completion.</returns>
		public async Task<PartialChatCompletionResult> CreateStreamingChatCompletionAsync(
			LLModelDescriptor model,
			IEnumerable<IMessage> messages,
			IEnumerable<CompletionProperty>? properties = null,
			OutputFormatDefinition? outputFormatDefinition = null,
			IEnumerable<ITool>? tools = null,
			bool validateCapabilities = false,
			CancellationToken cancellationToken = default)
		{
			ValidateChatCompletionParameters(model,
				ref messages,
				true,
				1,
				ref properties,
				ref outputFormatDefinition,
				ref tools,
				validateCapabilities);

			return await CreateStreamingChatCompletionsOverrideAsync(
				model,
				messages,
				1,
				properties,
				outputFormatDefinition,
				tools,
				cancellationToken);
		}

		/// <summary>
		/// Creates the chat completions using the specified model, messages, count, properties, native output format definition and tools.
		/// </summary>
		/// <param name="model">The model to use for the chat completion. Must be non-<see langword="null"/>.</param>
		/// <param name="messages">The messages to use for the chat completion. Must be non-empty.</param>
		/// <param name="count">The count of completions to create. Must be at least 1.</param>
		/// <param name="properties">The properties that affects the result message. Can be <see langword="null"/> to use the default properties.</param>
		/// <param name="outputFormatDefinition">
		/// The native output format definition that should be used to natively configure the output format.
		/// Can be <see langword="null"/> to use the empty output format definition.
		/// </param>
		/// <param name="tools">The available tools that can be called by model. Can be <see langword="null"/> to provide no tools.</param>
		/// <param name="validateCapabilities">Whether to validate the capabilities of the client and model before creating the completions. Default is <see langword="false"/>.</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>The <see cref="ChatCompletionResult"/> that contains the chat completions.</returns>
		public async Task<ChatCompletionResult> CreateChatCompletionsAsync(
			LLModelDescriptor model,
			IEnumerable<IMessage> messages,
			int count,
			IEnumerable<CompletionProperty>? properties = null,
			OutputFormatDefinition? outputFormatDefinition = null,
			IEnumerable<ITool>? tools = null,
			bool validateCapabilities = false,
			CancellationToken cancellationToken = default)
		{
			ValidateChatCompletionParameters(model,
				ref messages,
				false,
				count,
				ref properties,
				ref outputFormatDefinition,
				ref tools,
				validateCapabilities);

			return await CreateChatCompletionsOverrideAsync(
				model,
				messages,
				count,
				properties,
				outputFormatDefinition,
				tools,
				cancellationToken);
		}

		/// <summary>
		/// Creates the streaming chat completions using the specified model, messages, count, properties, native output format definition and tools.
		/// </summary>
		/// <param name="model">The model to use for the chat completion. Must be non-<see langword="null"/>.</param>
		/// <param name="messages">The messages to use for the chat completion. Must be non-empty.</param>
		/// <param name="count">The count of completions to create. Must be at least 1.</param>
		/// <param name="properties">The properties that affects the result message. Can be <see langword="null"/> to use the default properties.</param>
		/// <param name="outputFormatDefinition">
		/// The native output format definition that should be used to natively configure the output format.
		/// Can be <see langword="null"/> to use the empty output format definition.
		/// </param>
		/// <param name="tools">The available tools that can be called by model. Can be <see langword="null"/> to provide no tools.</param>
		/// <param name="validateCapabilities">Whether to validate the capabilities of the client and model before creating the completions. Default is <see langword="false"/>.</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>The <see cref="PartialChatCompletionResult"/> that contains the streaming chat completions.</returns>
		public async Task<PartialChatCompletionResult> CreateStreamingChatCompletionsAsync(
			LLModelDescriptor model,
			IEnumerable<IMessage> messages,
			int count,
			IEnumerable<CompletionProperty>? properties = null,
			OutputFormatDefinition? outputFormatDefinition = null,
			IEnumerable<ITool>? tools = null,
			bool validateCapabilities = false,
			CancellationToken cancellationToken = default)
		{
			ValidateChatCompletionParameters(model,
				ref messages,
				true,
				count,
				ref properties,
				ref outputFormatDefinition,
				ref tools,
				validateCapabilities);

			return await CreateStreamingChatCompletionsOverrideAsync(
				model,
				messages,
				count,
				properties,
				outputFormatDefinition,
				tools,
				cancellationToken);
		}
	}
}