using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Clients.OpenAI;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Formats;
using RCLargeLanguageModels.Security;
using RCLargeLanguageModels.Tools;
using RCLargeLanguageModels.Json;
using RCLargeLanguageModels.Completions;
using System.Text.Json.Nodes;

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

		public override string Name => "deepseek";
		public override string DisplayName => "DeepSeek";

		/// <summary>
		/// Creates a new instance of the DeepSeek client.
		/// </summary>
		/// <param name="apiKey">The API key for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public DeepSeekClient(string apiKey, HttpClient? http = null) : base(BaseUri, apiKey)
		{
		}

		/// <summary>
		/// Creates a new instance of the DeepSeek client.
		/// </summary>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public DeepSeekClient(ITokenAccessor tokenAccessor, HttpClient? http = null) : base(BaseUri, tokenAccessor)
		{
		}

		/// <summary>
		/// Creates a new instance of the DeepSeek client.
		/// </summary>
		/// <param name="endpointUri">The endpoint URI.</param>
		/// <param name="apiKey">The API key for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public DeepSeekClient(string endpointUri, string apiKey, HttpClient? http = null) : base(endpointUri, apiKey, http)
		{
		}

		/// <summary>
		/// Creates a new instance of the DeepSeek client.
		/// </summary>
		/// <param name="endpointUri">The endpoint URI.</param>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public DeepSeekClient(string endpointUri, ITokenAccessor tokenAccessor, HttpClient? http = null) : base(endpointUri, tokenAccessor, http)
		{
		}

		/// <summary>
		/// Creates a new instance of the DeepSeek client.
		/// </summary>
		/// <param name="endpointConfig">The endpoint config.</param>
		/// <param name="apiKey">The API key for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public DeepSeekClient(LLMEndpointConfig endpointConfig, string apiKey, HttpClient? http = null) : base(endpointConfig, apiKey, http)
		{
		}

		/// <summary>
		/// Creates a new instance of the DeepSeek client.
		/// </summary>
		/// <param name="endpointConfig">The endpoint config.</param>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public DeepSeekClient(LLMEndpointConfig endpointConfig, ITokenAccessor tokenAccessor, HttpClient? http = null) : base(endpointConfig, tokenAccessor, http)
		{
		}

		protected override Task<LLModelDescriptor[]> ListModelsOverrideAsync(CancellationToken cancellationToken = default)
		{
			return Task.FromResult(new LLModelDescriptor[]
			{
				new LLModelDescriptor(this,
					"deepseek-chat", "DeepSeek Chat",
					LLMCapabilities.ChatWithTools | LLMCapabilities.SuffixCompletions | LLMCapabilities.StreamingCompletions,
						supportedOutputFormats: OutputFormatSupportSet.Text.With(OutputFormatType.Json)),

				new LLModelDescriptor(this,
					"deepseek-reasoner", "DeepSeek Reasoning",
					LLMCapabilities.ChatWithReasoningAndTools | LLMCapabilities.StreamingCompletions,
						supportedOutputFormats: OutputFormatSupportSet.Text.With(OutputFormatType.Json))
			});
		}

		protected override void PopulateBodyWithProperties(JsonObject body, LLModelDescriptor model,
			OutputFormatDefinition outputFormatDefinition, IEnumerable<ITool> tools, IEnumerable<CompletionProperty> properties)
		{
			if (outputFormatDefinition.Type == OutputFormatType.Json)
				body["response_format"] = new JsonObject
				{
					["type"] = "json_object"
				};
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
	}
}