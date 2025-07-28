using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RCLargeLanguageModels.Clients.OpenAI;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Formats;
using RCLargeLanguageModels.Security;
using RCLargeLanguageModels.Tools;
using RCLargeLanguageModels.Json;
using RCLargeLanguageModels.Completions;

namespace RCLargeLanguageModels.Clients.Deepseek
{
	public class DeepSeekEndpointConfig : LLMEndpointConfig
	{
		public DeepSeekEndpointConfig(string baseUri) : base(baseUri)
		{
		}

		public override string GenerateChatCompletion => BaseUri + "/chat/completions";
		public override string ListModels => BaseUri + "/models";
	}

	[LLMClient]
	public class DeepSeekClient : OpenAICompatibleClient
	{
		/// <summary>
		/// The name of the API key in the token storage.
		/// </summary>
		public const string ApiKeyName = "deepseek-api-key";

		/// <summary>
		/// The base URI of the DeepSeek API.
		/// </summary>
		public const string BaseUri = "https://api.deepseek.com/beta";

		public override string Name => "deepseek";
		public override string DisplayName => "DeepSeek";

		/// <summary>
		/// Creates a new instance of the DeepSeek client.
		/// </summary>
		/// <param name="apiKey">The API key for authentication.</param>
		public DeepSeekClient(string apiKey) : base(BaseUri, apiKey)
		{
		}

		/// <summary>
		/// Creates a new instance of the DeepSeek client.
		/// </summary>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		[LLMClientConstructor]
		public DeepSeekClient([LLMAPIKey(ApiKeyName)] ITokenAccessor tokenAccessor) : base(BaseUri, tokenAccessor)
		{
		}

		/// <summary>
		/// Creates a new instance of the DeepSeek client.
		/// </summary>
		/// <param name="endpointUri">The endpoint URI.</param>
		/// <param name="apiKey">The API key for authentication.</param>
		public DeepSeekClient(string endpointUri, string apiKey) : base(endpointUri, apiKey)
		{
		}

		/// <summary>
		/// Creates a new instance of the DeepSeek client.
		/// </summary>
		/// <param name="endpointUri">The endpoint URI.</param>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		public DeepSeekClient(string endpointUri, ITokenAccessor tokenAccessor) : base(endpointUri, tokenAccessor)
		{
		}

		/// <summary>
		/// Creates a new instance of the DeepSeek client.
		/// </summary>
		/// <param name="endpointConfig">The endpoint config.</param>
		/// <param name="apiKey">The API key for authentication.</param>
		public DeepSeekClient(LLMEndpointConfig endpointConfig, string apiKey) : base(endpointConfig, apiKey)
		{
		}

		/// <summary>
		/// Creates a new instance of the DeepSeek client.
		/// </summary>
		/// <param name="endpointConfig">The endpoint config.</param>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		public DeepSeekClient(LLMEndpointConfig endpointConfig, ITokenAccessor tokenAccessor) : base(endpointConfig, tokenAccessor)
		{
		}

		protected override Task<LLModelDescriptor[]> ListModelsOverrideAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult(new LLModelDescriptor[]
			{
				new LLModelDescriptor(this,
					"deepseek-chat", "DeepSeek V3",
					LLMCapabilities.ChatWithTools | LLMCapabilities.SuffixCompletions | LLMCapabilities.StreamingCompletions,
						supportedOutputFormats: OutputFormatSupportSet.Text.With(OutputFormatType.Json)),

				// Now DeepSeek-R1 supports tools and structured outputs (28 May, 2025)!
				new LLModelDescriptor(this,
					"deepseek-reasoner", "DeepSeek-R1",
					LLMCapabilities.ChatWithReasoningAndTools | LLMCapabilities.StreamingCompletions,
						supportedOutputFormats: OutputFormatSupportSet.Text.With(OutputFormatType.Json))
			});
		}

		public override ICompletionProperties ConvertOrCreateCompletionProperties(ICompletionProperties chatProperties)
		{
			if (chatProperties is null)
				return new DeepSeekCompletionProperties();

			if (chatProperties is DeepSeekCompletionProperties ds)
				return ds;

			return new DeepSeekCompletionProperties
			{
				Temperature = chatProperties.Temperature,
				MaxTokens = chatProperties.MaxTokens,
				TopP = chatProperties.TopP,
				Stop = chatProperties.Stop
			};
		}

		protected override void PopulateBodyWithProperties(JObject body, LLModelDescriptor model, OutputFormatDefinition outputFormatDefinition, IEnumerable<ITool> tools, ICompletionProperties properties)
		{
			var conv = (DeepSeekCompletionProperties)properties;

			body.AddIfNotNull("frequency_penalty", conv.FrequencyPenalty);
			body.AddIfNotNull("presence_penalty", conv.PresencePenalty);
			body.AddIfNotNull("max_tokens", conv.MaxTokens);
			body.AddIfNotNull("stop", conv.Stop?.ToArray());
			body.AddIfNotNull("temperature", conv.Temperature);
			body.AddIfNotNull("top_p", conv.TopP);

			if (outputFormatDefinition.Type == OutputFormatType.Json)
				body["response_format"] = new JObject
				{
					["type"] = "json_object"
				};
		}

		protected override JObject BuildMessage(IMessage message, bool isLast)
		{
			var obj = base.BuildMessage(message, isLast);

			if (isLast && message is IAssistantMessage assistantMessage)
			{
				// Complete the assistant message using the beta feature
				obj["prefix"] = true;
				if (!string.IsNullOrEmpty(assistantMessage.ReasoningContent))
					obj["reasoning_content"] = assistantMessage.ReasoningContent;
			}

			return obj;
		}
	}
}