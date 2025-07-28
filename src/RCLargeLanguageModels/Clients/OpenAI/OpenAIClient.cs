using RCLargeLanguageModels.Security;

namespace RCLargeLanguageModels.Clients.OpenAI
{
	[LLMClient]
	public class OpenAIClient : OpenAICompatibleClient
	{
		/// <summary>
		/// The name of the API key in the token storage.
		/// </summary>
		public const string ApiKeyName = "openai-api-key";

		/// <summary>
		/// The base URI of the OpenAI API.
		/// </summary>
		public const string BaseUri = "https://api.openai.com";

		public override string Name => "openai";
		public override string DisplayName => "OpenAI";

		/// <summary>
		/// Creates a new instance of the OpenAI client.
		/// </summary>
		/// <param name="apiKey">The API key for authentication.</param>
		public OpenAIClient(string apiKey) : base(BaseUri, apiKey)
		{
		}

		/// <summary>
		/// Creates a new instance of the OpenAI client.
		/// </summary>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		[LLMClientConstructor]
		public OpenAIClient([LLMAPIKey(ApiKeyName)] ITokenAccessor tokenAccessor) : base(BaseUri, tokenAccessor)
		{
		}

		/// <summary>
		/// Creates a new instance of the OpenAI client.
		/// </summary>
		/// <param name="endpointUri">The endpoint URI.</param>
		/// <param name="apiKey">The API key for authentication.</param>
		public OpenAIClient(string endpointUri, string apiKey) : base(endpointUri, apiKey)
		{
		}

		/// <summary>
		/// Creates a new instance of the OpenAI client.
		/// </summary>
		/// <param name="endpointUri">The endpoint URI.</param>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		public OpenAIClient(string endpointUri, ITokenAccessor tokenAccessor) : base(endpointUri, tokenAccessor)
		{
		}

		/// <summary>
		/// Creates a new instance of the OpenAI client.
		/// </summary>
		/// <param name="endpointConfig">The endpoint config.</param>
		/// <param name="apiKey">The API key for authentication.</param>
		public OpenAIClient(LLMEndpointConfig endpointConfig, string apiKey) : base(endpointConfig, apiKey)
		{
		}

		/// <summary>
		/// Creates a new instance of the OpenAI client.
		/// </summary>
		/// <param name="endpointConfig">The endpoint config.</param>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		public OpenAIClient(LLMEndpointConfig endpointConfig, ITokenAccessor tokenAccessor) : base(endpointConfig, tokenAccessor)
		{
		}
	}
}