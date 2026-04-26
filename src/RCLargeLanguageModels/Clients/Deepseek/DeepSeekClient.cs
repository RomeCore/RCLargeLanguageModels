using RCLargeLanguageModels.Clients.OpenAI;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Completions.Properties;
using RCLargeLanguageModels.Formats;
using RCLargeLanguageModels.Json;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Metadata;
using RCLargeLanguageModels.Security;
using RCLargeLanguageModels.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

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

	public class DeepSeekClient : OpenAICompatibleClient
	{
		/// <summary>
		/// The base URI of the DeepSeek API.
		/// </summary>
		public const string BaseUri = "https://api.deepseek.com/beta";

		private void SetDefaultName()
		{
			Name = "deepseek";
			DisplayName = "DeepSeek";
		}

		/// <summary>
		/// Creates a new instance of the DeepSeek client.
		/// </summary>
		/// <param name="apiKey">The API key for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public DeepSeekClient(string apiKey, HttpClient? http = null) : base(BaseUri, apiKey)
		{
			SetDefaultName();
		}

		/// <summary>
		/// Creates a new instance of the DeepSeek client.
		/// </summary>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public DeepSeekClient(ITokenAccessor tokenAccessor, HttpClient? http = null) : base(BaseUri, tokenAccessor)
		{
			SetDefaultName();
		}

		/// <summary>
		/// Creates a new instance of the DeepSeek client.
		/// </summary>
		/// <param name="endpointUri">The endpoint URI.</param>
		/// <param name="apiKey">The API key for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public DeepSeekClient(string endpointUri, string apiKey, HttpClient? http = null) : base(endpointUri, apiKey, http)
		{
			SetDefaultName();
		}

		/// <summary>
		/// Creates a new instance of the DeepSeek client.
		/// </summary>
		/// <param name="endpointUri">The endpoint URI.</param>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public DeepSeekClient(string endpointUri, ITokenAccessor tokenAccessor, HttpClient? http = null) : base(endpointUri, tokenAccessor, http)
		{
			SetDefaultName();
		}

		/// <summary>
		/// Creates a new instance of the DeepSeek client.
		/// </summary>
		/// <param name="endpointConfig">The endpoint config.</param>
		/// <param name="apiKey">The API key for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public DeepSeekClient(LLMEndpointConfig endpointConfig, string apiKey, HttpClient? http = null) : base(endpointConfig, apiKey, http)
		{
			SetDefaultName();
		}

		/// <summary>
		/// Creates a new instance of the DeepSeek client.
		/// </summary>
		/// <param name="endpointConfig">The endpoint config.</param>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public DeepSeekClient(LLMEndpointConfig endpointConfig, ITokenAccessor tokenAccessor, HttpClient? http = null) : base(endpointConfig, tokenAccessor, http)
		{
			SetDefaultName();
		}

		protected override Task<LLModelDescriptor[]> ListModelsOverrideAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult(new LLModelDescriptor[]
			{
				new LLModelDescriptor(this,
					"deepseek-v4-flash", "DeepSeek Flash",
					LLMCapabilities.ChatWithReasoningAndTools | LLMCapabilities.SuffixCompletions | LLMCapabilities.StreamingCompletions,
						supportedOutputFormats: OutputFormatSupportSet.Text.With(OutputFormatType.Json)),

				new LLModelDescriptor(this,
					"deepseek-v4-pro", "DeepSeek Pro",
					LLMCapabilities.ChatWithReasoningAndTools | LLMCapabilities.SuffixCompletions | LLMCapabilities.StreamingCompletions,
						supportedOutputFormats: OutputFormatSupportSet.Text.With(OutputFormatType.Json))
			});
		}

		protected override KeyValuePair<string, JsonNode>? SerializeProperty(CompletionProperty property)
		{
			if (property is ReasoningProperty)
				return null;

			return base.SerializeProperty(property);
		}

		protected override void PopulateBodyWithProperties(JsonObject body, LLModelDescriptor model,
			OutputFormatDefinition outputFormatDefinition, IEnumerable<ITool> tools, IEnumerable<CompletionProperty> properties)
		{
			base.PopulateBodyWithProperties(body, model, outputFormatDefinition, tools, properties);

			if (outputFormatDefinition.Type == OutputFormatType.Json)
				body["response_format"] = new JsonObject
				{
					["type"] = "json_object"
				};

			if (properties.OfType<ReasoningProperty>().FirstOrDefault() is ReasoningProperty rp)
			{
				body["thinking"] = new JsonObject
				{
					["type"] = rp.Value ? "enabled" : "disabled",
				};
				if (rp.Value)
					body["reasoning_effort"] = rp.Effort switch
					{
						ReasoningEffort.Default => "high",
						ReasoningEffort.None => "high",
						ReasoningEffort.Minimal => "high",
						ReasoningEffort.Medium => "high",
						ReasoningEffort.High => "high",
						ReasoningEffort.XHigh => "max",
						ReasoningEffort.Max => "max",
						_ => "high"
					};
			}
		}

		protected override JsonObject BuildAssistantMessage(IAssistantMessage message, List<IMessage> list, int messageIndex)
		{
			var res = base.BuildAssistantMessage(message, list, messageIndex);

			bool hasUserMessageAfter = false;
			for (int i = messageIndex + 1; i < list.Count; i++)
				if (list[i] is IUserMessage)
				{
					hasUserMessageAfter = true;
					break;
				}

			if (!hasUserMessageAfter)
			{
				if (messageIndex == list.Count - 1)
					res["prefix"] = true;
				if (!string.IsNullOrEmpty(message.ReasoningContent))
					res["reasoning_content"] = message.ReasoningContent;
			}

			return res;
		}

		protected override IUsageMetadata GetUsageMetadata(JsonObject usage)
		{
			var promptTokens = usage["prompt_tokens"]?.GetValue<int>() ?? 0;
			var promptCHTokens = usage["prompt_cache_hit_tokens"]?.GetValue<int>() ?? 0;
			var promptCMTokens = usage["prompt_cache_miss_tokens"]?.GetValue<int>() ?? 0;
			var completionTokens = usage["completion_tokens"]?.GetValue<int>() ?? 0;

			if (promptCHTokens != 0 && promptCMTokens != 0)
				return new UsageCacheMetadata(promptCHTokens, promptCMTokens, completionTokens, 0);

			return new UsageMetadata(promptTokens, completionTokens);
		}
	}
}