using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Clients.OpenAI;
using RCLargeLanguageModels.Security;
using static System.Net.WebRequestMethods;

namespace RCLargeLanguageModels.Clients.Novita
{
	public class NovitaEndpointConfig : LLMEndpointConfig
	{
		public NovitaEndpointConfig(string baseUri) : base(baseUri)
		{
		}

		public override string GenerateChatCompletion => BaseUri + "/chat/completions";
		public override string ListModels => BaseUri + "/models";
	}

	/// <summary>
	/// The https://novita.ai/ client
	/// </summary>
	public class NovitaClient : OpenAICompatibleClient
	{
		/// <summary>
		/// The name of the API key in the token storage.
		/// </summary>
		public const string ApiKeyName = "novita-api-key";

		/// <summary>
		/// The base URI of the Novita API.
		/// </summary>
		public const string BaseUri = "https://api.novita.ai/v3/openai";

		private void SetDefaultName()
		{
			Name = "novita";
			DisplayName = "Novita";
		}

		/// <summary>
		/// Creates a new instance of the Novita client.
		/// </summary>
		/// <param name="apiKey">The API key for authentication.</param>
		public NovitaClient(string apiKey) : base(BaseUri, apiKey)
		{
			SetDefaultName();
		}

		/// <summary>
		/// Creates a new instance of the Novita client.
		/// </summary>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		public NovitaClient(ITokenAccessor tokenAccessor) : base(BaseUri, tokenAccessor)
		{
			SetDefaultName();
		}

		/// <summary>
		/// Creates a new instance of the Novita client.
		/// </summary>
		/// <param name="apiKey">The API key for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public NovitaClient(string apiKey, HttpClient? http) : base(BaseUri, apiKey)
		{
			SetDefaultName();
		}

		/// <summary>
		/// Creates a new instance of the Novita client.
		/// </summary>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public NovitaClient(ITokenAccessor tokenAccessor, HttpClient? http) : base(BaseUri, tokenAccessor, http)
		{
			SetDefaultName();
		}

		/// <summary>
		/// Creates a new instance of the Novita client.
		/// </summary>
		/// <param name="endpointUri">The endpoint URI.</param>
		/// <param name="apiKey">The API key for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public NovitaClient(string endpointUri, string apiKey, HttpClient? http = null) : base(endpointUri, apiKey, http)
		{
			SetDefaultName();
		}

		/// <summary>
		/// Creates a new instance of the Novita client.
		/// </summary>
		/// <param name="endpointUri">The endpoint URI.</param>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public NovitaClient(string endpointUri, ITokenAccessor tokenAccessor, HttpClient? http = null) : base(endpointUri, tokenAccessor, http)
		{
			SetDefaultName();
		}

		/// <summary>
		/// Creates a new instance of the Novita client.
		/// </summary>
		/// <param name="endpointConfig">The endpoint config.</param>
		/// <param name="apiKey">The API key for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public NovitaClient(LLMEndpointConfig endpointConfig, string apiKey, HttpClient? http = null) : base(endpointConfig, apiKey, http)
		{
			SetDefaultName();
		}

		/// <summary>
		/// Creates a new instance of the Novita client.
		/// </summary>
		/// <param name="endpointConfig">The endpoint config.</param>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public NovitaClient(LLMEndpointConfig endpointConfig, ITokenAccessor tokenAccessor, HttpClient? http = null) : base(endpointConfig, tokenAccessor, http)
		{
			SetDefaultName();
		}

		protected override async Task<LLModelDescriptor[]> ListModelsOverrideAsync(CancellationToken cancellationToken = default)
		{
			var descriptors = await base.ListModelsOverrideAsync(cancellationToken);
			var result = new List<LLModelDescriptor>();

			foreach (var descriptor in descriptors)
			{
				var filled = NovitaModels.GetModelDescriptor(this, descriptor.Name) ?? descriptor;
				result.Add(filled);
			}

			return result.ToArray();
		}
	}
}