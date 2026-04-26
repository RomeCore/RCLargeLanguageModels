using System.Net.Http;
using RCLargeLanguageModels.Security;

namespace RCLargeLanguageModels.Clients.OpenAI
{
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

		private void SetDefaultName()
		{
			Name = "openai";
			DisplayName = "OpenAI";
		}

		/// <summary>
		/// Creates a new instance of the OpenAI client.
		/// </summary>
		/// <param name="apiKey">The API key for authentication.</param>
		public OpenAIClient(string apiKey) : base(BaseUri, apiKey)
		{
			SetDefaultName();
		}

		/// <summary>
		/// Creates a new instance of the OpenAI client.
		/// </summary>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		public OpenAIClient(ITokenAccessor tokenAccessor) : base(BaseUri, tokenAccessor)
		{
			SetDefaultName();
		}

		/// <summary>
		/// Creates a new instance of the OpenAI client.
		/// </summary>
		/// <param name="apiKey">The API key for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public OpenAIClient(string apiKey, HttpClient? http) : base(BaseUri, apiKey, http)
		{
			SetDefaultName();
		}

		/// <summary>
		/// Creates a new instance of the OpenAI client.
		/// </summary>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public OpenAIClient(ITokenAccessor tokenAccessor, HttpClient? http) : base(BaseUri, tokenAccessor, http)
		{
			SetDefaultName();
		}

		/// <summary>
		/// Creates a new instance of the OpenAI client.
		/// </summary>
		/// <param name="endpointUri">The endpoint URI.</param>
		/// <param name="apiKey">The API key for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public OpenAIClient(string endpointUri, string apiKey, HttpClient? http = null) : base(endpointUri, apiKey, http)
		{
			SetDefaultName();
		}

		/// <summary>
		/// Creates a new instance of the OpenAI client.
		/// </summary>
		/// <param name="endpointUri">The endpoint URI.</param>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public OpenAIClient(string endpointUri, ITokenAccessor tokenAccessor, HttpClient? http = null) : base(endpointUri, tokenAccessor, http)
		{
			SetDefaultName();
		}

		/// <summary>
		/// Creates a new instance of the OpenAI client.
		/// </summary>
		/// <param name="endpointConfig">The endpoint config.</param>
		/// <param name="apiKey">The API key for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public OpenAIClient(LLMEndpointConfig endpointConfig, string apiKey, HttpClient? http = null) : base(endpointConfig, apiKey, http)
		{
			SetDefaultName();
		}

		/// <summary>
		/// Creates a new instance of the OpenAI client.
		/// </summary>
		/// <param name="endpointConfig">The endpoint config.</param>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public OpenAIClient(LLMEndpointConfig endpointConfig, ITokenAccessor tokenAccessor, HttpClient? http = null) : base(endpointConfig, tokenAccessor, http)
		{
			SetDefaultName();
		}
	}
}