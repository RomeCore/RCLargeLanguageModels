using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Formats;
using RCLargeLanguageModels.Security;
using RCLargeLanguageModels.Statistics;
using RCLargeLanguageModels.Tools;
using RCLargeLanguageModels.Utilities;
using Serilog;
using RCLargeLanguageModels.Completions;
using System.Net.Http;
using System.Collections.Immutable;
using RCLargeLanguageModels.Completions.Properties;
using System.Text.Json.Nodes;
using System.Text.Json;
using RCLargeLanguageModels.Embeddings;
using RCLargeLanguageModels.Exceptions;

namespace RCLargeLanguageModels.Clients.OpenAI
{
	/// <summary>
	/// Represents a client for interacting with the OpenAI-compatible API.
	/// </summary>
	/// <remarks>
	/// Mostly based on DeepSeek documentation, since OpenAI API is not yet available.
	/// </remarks>
	public partial class OpenAICompatibleClient : LLMClient
	{
		private readonly HttpClient _http;
		private readonly LLMEndpointConfig _endpoint;
		private readonly ITokenAccessor _apiKeyAccessor;

		public override string Name => "openai-compatible";
		public override string DisplayName => "OpenAI Compatible";

		/// <summary>
		/// Creates a new instance of the OpenAI-compatible client using the specified base URI and API key.
		/// </summary>
		/// <param name="baseUri">The base URI of the OpenAI-compatible API.</param>
		/// <param name="apiKey">The API key for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public OpenAICompatibleClient(string baseUri, string apiKey, HttpClient? http = null)
		{
			_apiKeyAccessor = new StringTokenAccessor(apiKey);
			_http = http ?? CreateHttpClient();
			_endpoint = new OpenAIEndpointConfig(baseUri ?? throw new ArgumentNullException(nameof(baseUri)));
		}

		/// <summary>
		/// Creates a new instance of the OpenAI-compatible client using the specified base URI and API key accessor.
		/// </summary>
		/// <param name="baseUri">The base URI of the OpenAI-compatible API.</param>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public OpenAICompatibleClient(string baseUri, ITokenAccessor tokenAccessor, HttpClient? http = null)
		{
			_apiKeyAccessor = tokenAccessor ?? throw new ArgumentNullException(nameof(tokenAccessor));
			_http = http ?? CreateHttpClient();
			_endpoint = new OpenAIEndpointConfig(baseUri ?? throw new ArgumentNullException(nameof(baseUri)));
		}

		/// <summary>
		/// Creates a new instance of the OpenAI-compatible client using the specified endpoint configuration and API key.
		/// </summary>
		/// <param name="endpointConfig">The endpoint configuration for the OpenAI-compatible API.</param>
		/// <param name="apiKey">The API key for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public OpenAICompatibleClient(LLMEndpointConfig endpointConfig, string apiKey, HttpClient? http = null)
		{
			_apiKeyAccessor = new StringTokenAccessor(apiKey);
			_http = http ?? CreateHttpClient();
			_endpoint = endpointConfig ?? throw new ArgumentNullException(nameof(endpointConfig));
		}

		/// <summary>
		/// Creates a new instance of the OpenAI-compatible client using the specified endpoint configuration and API key accessor.
		/// </summary>
		/// <param name="endpointConfig">The endpoint configuration for the OpenAI-compatible API.</param>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public OpenAICompatibleClient(LLMEndpointConfig endpointConfig, ITokenAccessor tokenAccessor, HttpClient? http = null)
		{
			_apiKeyAccessor = tokenAccessor ?? throw new ArgumentNullException(nameof(tokenAccessor));
			_http = http ?? CreateHttpClient();
			_endpoint = endpointConfig ?? throw new ArgumentNullException(nameof(endpointConfig));
		}

		protected virtual Dictionary<string, string> GetRequestHeaders()
		{
			return new Dictionary<string, string>
			{
				{ "Authorization", "Bearer " + _apiKeyAccessor.GetToken() }
			};
		}

		protected override async Task<LLModelDescriptor[]> ListModelsOverrideAsync(CancellationToken cancellationToken = default)
		{
			var headers = GetRequestHeaders();

			try
			{
				var response = await RequestUtility.GetResponseAsync(RequestType.Get, _endpoint.ListModels,
					null, _http, headers, cancellationToken);

				var responseContent = await response.ParseContentAsync<JsonObject>(cancellationToken);

				var models = responseContent["data"] as JsonArray;
				if (models == null)
					throw new LLMException("No models in response.");

				var result = new List<LLModelDescriptor>();
				foreach (var model in models)
				{
					var id = model!["id"]?.GetValue<string>();
					result.Add(new LLModelDescriptor(this, id!));
				}

				return result.ToArray();
			}
			catch (Exception ex)
			{
				throw new LLMException("Failed to list models.", ex);
			}
		}
	}
}