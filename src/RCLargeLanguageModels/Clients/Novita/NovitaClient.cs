using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Clients.OpenAI;
using RCLargeLanguageModels.Security;

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
	[LLMClient]
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

		public override string Name => "novita";
		public override string DisplayName => "Novita";

		/// <summary>
		/// Creates a new instance of the Novita client.
		/// </summary>
		/// <param name="apiKey">The API key for authentication.</param>
		public NovitaClient(string apiKey) : base(BaseUri, apiKey)
		{
		}

		/// <summary>
		/// Creates a new instance of the Novita client.
		/// </summary>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		[LLMClientConstructor]
		public NovitaClient([LLMAPIKey(ApiKeyName)] ITokenAccessor tokenAccessor) : base(BaseUri, tokenAccessor)
		{
		}

		/// <summary>
		/// Creates a new instance of the Novita client.
		/// </summary>
		/// <param name="endpointUri">The endpoint URI.</param>
		/// <param name="apiKey">The API key for authentication.</param>
		public NovitaClient(string endpointUri, string apiKey) : base(endpointUri, apiKey)
		{
		}

		/// <summary>
		/// Creates a new instance of the Novita client.
		/// </summary>
		/// <param name="endpointUri">The endpoint URI.</param>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		public NovitaClient(string endpointUri, ITokenAccessor tokenAccessor) : base(endpointUri, tokenAccessor)
		{
		}

		/// <summary>
		/// Creates a new instance of the Novita client.
		/// </summary>
		/// <param name="endpointConfig">The endpoint config.</param>
		/// <param name="apiKey">The API key for authentication.</param>
		public NovitaClient(LLMEndpointConfig endpointConfig, string apiKey) : base(endpointConfig, apiKey)
		{
		}

		/// <summary>
		/// Creates a new instance of the Novita client.
		/// </summary>
		/// <param name="endpointConfig">The endpoint config.</param>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		public NovitaClient(LLMEndpointConfig endpointConfig, ITokenAccessor tokenAccessor) : base(endpointConfig, tokenAccessor)
		{
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