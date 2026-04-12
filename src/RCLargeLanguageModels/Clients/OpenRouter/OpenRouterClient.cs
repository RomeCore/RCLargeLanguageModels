using RCLargeLanguageModels.Clients.OpenAI;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Security;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;

namespace RCLargeLanguageModels.Clients.OpenRouter
{
	public class OpenRouterEndpointConfig : LLMEndpointConfig
	{
		public OpenRouterEndpointConfig(string baseUri) : base(baseUri)
		{
		}

		public override string GenerateChatCompletion => BaseUri + "/chat/completions";
		public override string ListModels => BaseUri + "/models";
	}

	public class OpenRouterClient : OpenAICompatibleClient
	{
		/// <summary>
		/// The base URI of the OpenRouter API.
		/// </summary>
		public const string BaseUri = "https://openrouter.ai/api/v1";

		public override string Name => "openrouter";
		public override string DisplayName => "OpenRouter";

		/// <summary>
		/// Creates a new instance of the OpenRouter client.
		/// </summary>
		/// <param name="apiKey">The API key for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public OpenRouterClient(string apiKey, HttpClient? http = null) : base(BaseUri, apiKey, http)
		{
		}

		/// <summary>
		/// Creates a new instance of the OpenRouter client.
		/// </summary>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public OpenRouterClient(ITokenAccessor tokenAccessor, HttpClient? http = null) : base(BaseUri, tokenAccessor, http)
		{
		}

		/// <summary>
		/// Creates a new instance of the OpenRouter client.
		/// </summary>
		/// <param name="endpointUri">The endpoint URI.</param>
		/// <param name="apiKey">The API key for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public OpenRouterClient(string endpointUri, string apiKey, HttpClient? http = null) : base(endpointUri, apiKey, http)
		{
		}

		/// <summary>
		/// Creates a new instance of the OpenRouter client.
		/// </summary>
		/// <param name="endpointUri">The endpoint URI.</param>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public OpenRouterClient(string endpointUri, ITokenAccessor tokenAccessor, HttpClient? http = null) : base(endpointUri, tokenAccessor, http)
		{
		}

		/// <summary>
		/// Creates a new instance of the OpenRouter client.
		/// </summary>
		/// <param name="endpointConfig">The endpoint config.</param>
		/// <param name="apiKey">The API key for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public OpenRouterClient(LLMEndpointConfig endpointConfig, string apiKey, HttpClient? http = null) : base(endpointConfig, apiKey, http)
		{
		}

		/// <summary>
		/// Creates a new instance of the OpenRouter client.
		/// </summary>
		/// <param name="endpointConfig">The endpoint config.</param>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public OpenRouterClient(LLMEndpointConfig endpointConfig, ITokenAccessor tokenAccessor, HttpClient? http = null) : base(endpointConfig, tokenAccessor, http)
		{
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
				if (!string.IsNullOrEmpty(message.ReasoningContent))
					res["reasoning"] = message.ReasoningContent;
			}

			return res;
		}
	}
}