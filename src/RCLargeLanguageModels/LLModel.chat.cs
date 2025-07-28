using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Formats;
using RCLargeLanguageModels.PropertyInjectors;
using RCLargeLanguageModels.Tasks;
using RCLargeLanguageModels.Tools;
using RCLargeLanguageModels.Completions;

namespace RCLargeLanguageModels
{
	public partial class LLModel
	{
		private async Task<ChatCompletionResult> ChatPrivateAsync(
			IEnumerable<IMessage> messages,
			int count,
			ICompletionProperties properties = null,
			OutputFormatDefinition outputFormatDefinition = null,
			IEnumerable<ITool> tools = null,
			IEnumerable<ILLModelPropertyInjector> injectors = null,
			TaskQueueParameters queueParameters = null,
			CancellationToken cancellationToken	= default)
		{
			if (Capabilities != LLMCapabilities.Unknown && !Capabilities.HasFlag(LLMCapabilities.ChatCompletions))
				throw new InvalidOperationException("Chat completions are not supported by this model.");
			if (messages == null)
				throw new ArgumentNullException(nameof(messages));
			if (!messages.Any())
				throw new ArgumentException("Messages cannot be empty.", nameof(messages));

			if (injectors != null)
				foreach (var injector in injectors)
					injector?.InjectChatCompletion(this, ref messages, ref count, ref properties, ref tools, ref outputFormatDefinition);

			return await TaskQueueMaster.EnqueueAsync<ChatCompletionResult>(queueParameters, async () =>
			{
				return await Client.CreateChatCompletionsAsync(
					Descriptor,
					messages,
					count,
					properties,
					outputFormatDefinition,
					tools,
					cancellationToken);
			}, cancellationToken: cancellationToken);
		}
		
		private async Task<PartialChatCompletionResult> ChatStreamingPrivateAsync(
			IEnumerable<IMessage> messages,
			int count,
			ICompletionProperties properties = null,
			OutputFormatDefinition outputFormatDefinition = null,
			IEnumerable<ITool> tools = null,
			IEnumerable<ILLModelPropertyInjector> injectors = null,
			TaskQueueParameters queueParameters = null,
			CancellationToken cancellationToken	= default)
		{
			if (Capabilities != LLMCapabilities.Unknown && !Capabilities.HasFlag(LLMCapabilities.ChatCompletions))
				throw new InvalidOperationException("Chat completions are not supported by this model.");
			if (messages == null)
				throw new ArgumentNullException(nameof(messages));
			if (!messages.Any())
				throw new ArgumentException("Messages cannot be empty.", nameof(messages));

			if (injectors != null)
				foreach (var injector in injectors)
					injector?.InjectChatCompletion(this, ref messages, ref count, ref properties, ref tools, ref outputFormatDefinition);

			return await TaskQueueMaster.EnqueueAsync<PartialChatCompletionResult>(queueParameters, async () =>
			{
				return await Client.CreateStreamingChatCompletionsAsync(
					Descriptor,
					messages,
					count,
					properties,
					outputFormatDefinition,
					tools,
					cancellationToken);
			}, cancellationToken: cancellationToken);
		}

		/// <summary>
		/// Creates a chat completion using the provided messages.
		/// </summary>
		/// <param name="messages">The messages to send to the model.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The chat completion result.</returns>
		public Task<ChatCompletionResult> ChatAsync(
			IEnumerable<IMessage> messages,
			CancellationToken cancellationToken = default)
		{
			return ChatPrivateAsync(
				messages,
				1,
				CompletionProperties,
				OutputFormatDefinition,
				Tools,
				Injectors,
				QueueParameters,
				cancellationToken);
		}

		/// <summary>
		/// Creates multiple chat completions using the provided messages.
		/// </summary>
		/// <param name="messages">The messages to send to the model.</param>
		/// <param name="count">The number of completions to generate.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The assistant's response messages.</returns>
		public async Task<ChatCompletionResult> ChatAsync(
			IEnumerable<IMessage> messages,
			int count,
			CancellationToken cancellationToken = default)
		{
			return await ChatPrivateAsync(
				messages,
				count,
				CompletionProperties,
				OutputFormatDefinition,
				Tools,
				Injectors,
				QueueParameters,
				cancellationToken);
		}

		/// <summary>
		/// Creates a chat completion with all parameters.
		/// </summary>
		/// <param name="messages">The messages to send to the model.</param>
		/// <param name="properties">The custom chat properties to use for this request.</param>
		/// <param name="outputFormatDefinition">The native output format definition to use for this request.</param>
		/// <param name="tools">The tools to make available to the model.</param>
		/// <param name="injectors">Additional property injectors to use for this request.</param>
		/// <param name="queueParameters">The custom queue parameters to use for this request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The chat completion result.</returns>
		public Task<ChatCompletionResult> ChatAsync(
			IEnumerable<IMessage> messages,
			OptionalParameter<ICompletionProperties> properties = null,
			OptionalParameter<OutputFormatDefinition> outputFormatDefinition = null,
			OptionalParameter<IEnumerable<ITool>> tools = null,
			OptionalParameter<IEnumerable<ILLModelPropertyInjector>> injectors = null,
			OptionalParameter<TaskQueueParameters> queueParameters = null,
			CancellationToken cancellationToken = default)
		{
			return ChatPrivateAsync(
				messages,
				1,
				properties.GetValue(CompletionProperties),
				outputFormatDefinition.GetValue(OutputFormatDefinition),
				tools.GetValue(Tools),
				injectors.GetValue(Injectors),
				queueParameters.GetValue(QueueParameters),
				cancellationToken);
		}

		/// <summary>
		/// Creates multiple chat completions with all parameters.
		/// </summary>
		/// <param name="messages">The messages to send to the model.</param>
		/// <param name="count">The number of completions to generate.</param>
		/// <param name="properties">The custom chat properties to use for this request.</param>
		/// <param name="outputFormatDefinition">The native output format definition to use for this request.</param>
		/// <param name="tools">The tools to make available to the model.</param>
		/// <param name="injectors">Additional property injectors to use for this request.</param>
		/// <param name="queueParameters">The custom queue parameters to use for this request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The assistant's response messages.</returns>
		public async Task<ChatCompletionResult> ChatAsync(
			IEnumerable<IMessage> messages,
			int count,
			OptionalParameter<ICompletionProperties> properties = null,
			OptionalParameter<OutputFormatDefinition> outputFormatDefinition = null,
			OptionalParameter<IEnumerable<ITool>> tools = null,
			OptionalParameter<IEnumerable<ILLModelPropertyInjector>> injectors = null,
			OptionalParameter<TaskQueueParameters> queueParameters = null,
			CancellationToken cancellationToken = default)
		{
			return await ChatPrivateAsync(
				messages,
				count,
				properties.GetValue(CompletionProperties),
				outputFormatDefinition.GetValue(OutputFormatDefinition),
				tools.GetValue(Tools),
				injectors.GetValue(Injectors),
				queueParameters.GetValue(QueueParameters),
				cancellationToken);
		}

		/// <summary>
		/// Creates a astreaming chat completion using the provided messages.
		/// </summary>
		/// <param name="messages">The messages to send to the model.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The streaming partial chat completion result.</returns>
		public Task<PartialChatCompletionResult> ChatStreamingAsync(
			IEnumerable<IMessage> messages,
			CancellationToken cancellationToken = default)
		{
			return ChatStreamingPrivateAsync(
				messages,
				1,
				CompletionProperties,
				OutputFormatDefinition,
				Tools,
				Injectors,
				QueueParameters,
				cancellationToken);
		}

		/// <summary>
		/// Creates multiple streaming chat completions using the provided messages.
		/// </summary>
		/// <param name="messages">The messages to send to the model.</param>
		/// <param name="count">The number of completions to generate.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The streaming partial assistant's response messages.</returns>
		public async Task<PartialChatCompletionResult> ChatStreamingAsync(
			IEnumerable<IMessage> messages,
			int count,
			CancellationToken cancellationToken = default)
		{
			return await ChatStreamingPrivateAsync(
				messages,
				count,
				CompletionProperties,
				OutputFormatDefinition,
				Tools,
				Injectors,
				QueueParameters,
				cancellationToken);
		}

		/// <summary>
		/// Creates a streaming chat completion with all parameters.
		/// </summary>
		/// <param name="messages">The messages to send to the model.</param>
		/// <param name="properties">The custom chat properties to use for this request.</param>
		/// <param name="outputFormatDefinition">The native output format definition to use for this request.</param>
		/// <param name="tools">The tools to make available to the model.</param>
		/// <param name="injectors">Additional property injectors to use for this request.</param>
		/// <param name="queueParameters">The custom queue parameters to use for this request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The streaming partial chat completion result.</returns>
		public Task<PartialChatCompletionResult> ChatStreamingAsync(
			IEnumerable<IMessage> messages,
			OptionalParameter<ICompletionProperties> properties = null,
			OptionalParameter<OutputFormatDefinition> outputFormatDefinition = null,
			OptionalParameter<IEnumerable<ITool>> tools = null,
			OptionalParameter<IEnumerable<ILLModelPropertyInjector>> injectors = null,
			OptionalParameter<TaskQueueParameters> queueParameters = null,
			CancellationToken cancellationToken = default)
		{
			return ChatStreamingPrivateAsync(
				messages,
				1,
				properties.GetValue(CompletionProperties),
				outputFormatDefinition.GetValue(OutputFormatDefinition),
				tools.GetValue(Tools),
				injectors.GetValue(Injectors),
				queueParameters.GetValue(QueueParameters),
				cancellationToken);
		}

		/// <summary>
		/// Creates multiple streaming chat completions with all parameters.
		/// </summary>
		/// <param name="messages">The messages to send to the model.</param>
		/// <param name="count">The number of completions to generate.</param>
		/// <param name="properties">The custom chat properties to use for this request.</param>
		/// <param name="outputFormatDefinition">The native output format definition to use for this request.</param>
		/// <param name="tools">The tools to make available to the model.</param>
		/// <param name="injectors">Additional property injectors to use for this request.</param>
		/// <param name="queueParameters">The custom queue parameters to use for this request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The streaming partial assistant's response messages.</returns>
		public async Task<PartialChatCompletionResult> ChatStreamingAsync(
			IEnumerable<IMessage> messages,
			int count,
			OptionalParameter<ICompletionProperties> properties = null,
			OptionalParameter<OutputFormatDefinition> outputFormatDefinition = null,
			OptionalParameter<IEnumerable<ITool>> tools = null,
			OptionalParameter<IEnumerable<ILLModelPropertyInjector>> injectors = null,
			OptionalParameter<TaskQueueParameters> queueParameters = null,
			CancellationToken cancellationToken = default)
		{
			return await ChatStreamingPrivateAsync(
				messages,
				count,
				properties.GetValue(CompletionProperties),
				outputFormatDefinition.GetValue(OutputFormatDefinition),
				tools.GetValue(Tools),
				injectors.GetValue(Injectors),
				queueParameters.GetValue(QueueParameters),
				cancellationToken);
		}
	}
}