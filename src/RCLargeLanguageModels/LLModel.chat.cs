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
			IEnumerable<CompletionProperty> properties,
			OutputFormatDefinition outputFormatDefinition,
			IEnumerable<ITool> tools,
			IEnumerable<ILLModelPropertyInjector> injectors,
			TaskQueueParameters queueParameters,
			bool validateCapabilities,
			CancellationToken cancellationToken)
		{
			if (validateCapabilities)
			{
				if (Capabilities != LLMCapabilities.Unknown && !Capabilities.HasFlag(LLMCapabilities.ChatCompletions))
					throw new InvalidOperationException("Chat completions are not supported by this model.");
			}

			if (messages == null)
				throw new ArgumentNullException(nameof(messages));
			if (!messages.Any())
				throw new ArgumentException("Messages cannot be empty.", nameof(messages));

			var injectorsList = injectors != null ? (injectors as IReadOnlyList<ILLModelPropertyInjector> ?? new List<ILLModelPropertyInjector>(injectors)) : null;
			if (injectors != null && injectorsList.Count > 0)
			{
				var messageList = messages as List<IMessage> ?? new List<IMessage>(messages);
				var propertiesList = properties as List<CompletionProperty> ?? new List<CompletionProperty>(properties);
				var toolSet = tools as ToolSet ?? new ToolSet(tools);

				var injectionParameters = new ChatCompletionInjectionParameters(this, messageList, count, propertiesList,
					toolSet, outputFormatDefinition);
				foreach (var injector in injectorsList)
					await injector.InjectChatCompletionAsync(injectionParameters);

				messages = injectionParameters.Messages;
				count = injectionParameters.Count;
				properties = injectionParameters.Properties;
				tools = injectionParameters.Tools;
				outputFormatDefinition = injectionParameters.OutputFormatDefinition;
			}

			return await TaskQueueMaster.EnqueueAsync<ChatCompletionResult>(queueParameters, async () =>
			{
				return await Client.CreateChatCompletionsAsync(
					Descriptor,
					messages,
					count,
					properties,
					outputFormatDefinition,
					tools,
					validateCapabilities,
					cancellationToken);
			}, cancellationToken: cancellationToken);
		}
		
		private async Task<PartialChatCompletionResult> ChatStreamingPrivateAsync(
			IEnumerable<IMessage> messages,
			int count,
			IEnumerable<CompletionProperty> properties,
			OutputFormatDefinition outputFormatDefinition,
			IEnumerable<ITool> tools,
			IEnumerable<ILLModelPropertyInjector> injectors,
			TaskQueueParameters queueParameters,
			bool validateCapabilities,
			CancellationToken cancellationToken)
		{
			if (validateCapabilities)
			{
				if (Capabilities != LLMCapabilities.Unknown && !Capabilities.HasFlag(LLMCapabilities.ChatCompletions))
					throw new InvalidOperationException("Chat completions are not supported by this model.");
			}

			if (messages == null)
				throw new ArgumentNullException(nameof(messages));
			if (!messages.Any())
				throw new ArgumentException("Messages cannot be empty.", nameof(messages));

			var injectorsList = injectors != null ? (injectors as IReadOnlyList<ILLModelPropertyInjector> ?? new List<ILLModelPropertyInjector>(injectors)) : null;
			if (injectors != null && injectorsList.Count > 0)
			{
				var messageList = messages as List<IMessage> ?? new List<IMessage>(messages);
				var propertiesList = properties as List<CompletionProperty> ?? new List<CompletionProperty>(properties);
				var toolSet = tools as ToolSet ?? new ToolSet(tools);

				var injectionParameters = new ChatCompletionInjectionParameters(this, messageList, count, propertiesList,
					toolSet, outputFormatDefinition);
				foreach (var injector in injectorsList)
					await injector.InjectChatCompletionAsync(injectionParameters);

				messages = injectionParameters.Messages;
				count = injectionParameters.Count;
				properties = injectionParameters.Properties;
				tools = injectionParameters.Tools;
				outputFormatDefinition = injectionParameters.OutputFormatDefinition;
			}

			return await TaskQueueMaster.EnqueueAsync<PartialChatCompletionResult>(queueParameters, async () =>
			{
				return await Client.CreateStreamingChatCompletionsAsync(
					Descriptor,
					messages,
					count,
					properties,
					outputFormatDefinition,
					tools,
					validateCapabilities,
					cancellationToken);
			}, cancellationToken: cancellationToken);
		}

		/// <summary>
		/// Creates multiple chat completions with all parameters.
		/// </summary>
		/// <param name="messages">The messages to send to the model.</param>
		/// <param name="properties">The custom chat properties to use for this request.</param>
		/// <param name="outputFormatDefinition">The native output format definition to use for this request.</param>
		/// <param name="tools">The tools to make available to the model.</param>
		/// <param name="injectors">Additional property injectors to use for this request.</param>
		/// <param name="queueParameters">The custom queue parameters to use for this request.</param>
		/// <param name="validateCapabilities">Whether to validate the model's and client's capabilities before making this request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The assistant's response messages.</returns>
		public Task<ChatCompletionResult> ChatAsync(
			IEnumerable<IMessage> messages,
			IEnumerable<CompletionProperty> properties = null,
			OutputFormatDefinition outputFormatDefinition = null,
			IEnumerable<ITool> tools = null,
			IEnumerable<ILLModelPropertyInjector> injectors = null,
			TaskQueueParameters queueParameters = null,
			bool validateCapabilities = false,
			CancellationToken cancellationToken = default)
		{
			return ChatPrivateAsync(
				messages,
				1,
				properties ?? CompletionProperties,
				outputFormatDefinition ?? OutputFormatDefinition,
				tools ?? Tools,
				injectors ?? Injectors,
				queueParameters ?? QueueParameters,
				validateCapabilities,
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
		/// <param name="validateCapabilities">Whether to validate the model's and client's capabilities before making this request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The assistant's response messages.</returns>
		public Task<ChatCompletionResult> ChatAsync(
			IEnumerable<IMessage> messages,
			int count,
			IEnumerable<CompletionProperty> properties = null,
			OutputFormatDefinition outputFormatDefinition = null,
			IEnumerable<ITool> tools = null,
			IEnumerable<ILLModelPropertyInjector> injectors = null,
			TaskQueueParameters queueParameters = null,
			bool validateCapabilities = false,
			CancellationToken cancellationToken = default)
		{
			return ChatPrivateAsync(
				messages,
				count,
				properties ?? CompletionProperties,
				outputFormatDefinition ?? OutputFormatDefinition,
				tools ?? Tools,
				injectors ?? Injectors,
				queueParameters ?? QueueParameters,
				validateCapabilities,
				cancellationToken);
		}

		/// <summary>
		/// Creates multiple streaming chat completions with all parameters.
		/// </summary>
		/// <param name="messages">The messages to send to the model.</param>
		/// <param name="properties">The custom chat properties to use for this request.</param>
		/// <param name="outputFormatDefinition">The native output format definition to use for this request.</param>
		/// <param name="tools">The tools to make available to the model.</param>
		/// <param name="injectors">Additional property injectors to use for this request.</param>
		/// <param name="queueParameters">The custom queue parameters to use for this request.</param>
		/// <param name="validateCapabilities">Whether to validate the model's and client's capabilities before making this request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The streaming partial assistant's response messages.</returns>
		public Task<PartialChatCompletionResult> ChatStreamingAsync(
			IEnumerable<IMessage> messages,
			IEnumerable<CompletionProperty> properties = null,
			OutputFormatDefinition outputFormatDefinition = null,
			IEnumerable<ITool> tools = null,
			IEnumerable<ILLModelPropertyInjector> injectors = null,
			TaskQueueParameters queueParameters = null,
			bool validateCapabilities = false,
			CancellationToken cancellationToken = default)
		{
			return ChatStreamingPrivateAsync(
				messages,
				1,
				properties ?? CompletionProperties,
				outputFormatDefinition ?? OutputFormatDefinition,
				tools ?? Tools,
				injectors ?? Injectors,
				queueParameters ?? QueueParameters,
				validateCapabilities,
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
		/// <param name="validateCapabilities">Whether to validate the model's and client's capabilities before making this request.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The streaming partial assistant's response messages.</returns>
		public Task<PartialChatCompletionResult> ChatStreamingAsync(
			IEnumerable<IMessage> messages,
			int count,
			IEnumerable<CompletionProperty> properties = null,
			OutputFormatDefinition outputFormatDefinition = null,
			IEnumerable<ITool> tools = null,
			IEnumerable<ILLModelPropertyInjector> injectors = null,
			TaskQueueParameters queueParameters = null,
			bool validateCapabilities = false,
			CancellationToken cancellationToken = default)
		{
			return ChatStreamingPrivateAsync(
				messages,
				count,
				properties ?? CompletionProperties,
				outputFormatDefinition ?? OutputFormatDefinition,
				tools ?? Tools,
				injectors ?? Injectors,
				queueParameters ?? QueueParameters,
				validateCapabilities,
				cancellationToken);
		}
	}
}