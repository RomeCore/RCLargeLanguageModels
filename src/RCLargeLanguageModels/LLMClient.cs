using System.Net.Http;
using System.Threading;
using RCLargeLanguageModels.Clients;
using RCLargeLanguageModels.Formats;
using RCLargeLanguageModels.Metadata;

namespace RCLargeLanguageModels
{
	/// <summary>
	/// Defines an endpoint configuration for a large language model (LLM) client.
	/// </summary>
	public class LLMEndpointConfig
	{
		/// <summary>
		/// The base URI of the endpoint.
		/// </summary>
		public string BaseUri { get; set; }

		/// <summary>
		/// The completed URI for listing available models. Can be empty if not supported.
		/// </summary>
		public virtual string ListModels => BaseUri + "/api/models";

		/// <summary>
		/// The completed URI for generating chat completions.
		/// </summary>
		public virtual string GenerateChatCompletion => BaseUri + "/api/chat/generate";
		
		/// <summary>
		/// The completed URI for generating completions.
		/// </summary>
		public virtual string GenerateCompletion => BaseUri + "/api/generate";

		/// <summary>
		/// The completed URI for generating embeddings.
		/// </summary>
		public virtual string GenerateEmbedding => BaseUri + "/api/embeddings";

		/// <summary>
		/// Creates a new instance of the <see cref="LLMEndpointConfig"/> class.
		/// </summary>
		/// <param name="baseUri">The base URI of the endpoint.</param>
		public LLMEndpointConfig(string baseUri)
		{
			BaseUri = baseUri;
		}
	}

	/// <summary>
	/// Represents a large language model (LLM) client that interacts with an LLM API.
	/// </summary>
	public abstract partial class LLMClient : IMetadataProvider
	{
		/// <summary>
		/// Gets the empty instance of the <see cref="LLMClient"/> class that returns a dummy content for all operations.
		/// </summary>
		public static LLMClient Empty { get; } = new EmptyLLMClient();

		/// <summary>
		/// Gets the name of the LLM client.
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Gets the display name of the LLM client.
		/// </summary>
		public virtual string DisplayName => Name;

		/// <summary>
		/// Gets the LLM client's API capabilities.
		/// </summary>
		public virtual LLMCapabilities Capabilities => LLMCapabilities.Unknown;

		/// <summary>
		/// Gets a set of natively supported output formats.
		/// </summary>
		public virtual OutputFormatSupportSet SupportedOutputFormats => OutputFormatSupportSet.Text;

		/// <summary>
		/// Gets the metadata collection associated with this client. Can contain various metadata, including API version, description, language support, etc.
		/// </summary>
		public virtual MetadataCollection Metadata => MetadataCollection.Empty;
		IMetadataCollection IMetadataProvider.Metadata => Metadata;

		public override string ToString()
		{
			if (Name == DisplayName)
				return $"LLM client \"{Name}\"";
			return $"LLM client \"{Name}\" ({DisplayName})";
		}

		/// <summary>
		/// Creates a configured <see cref="HttpClient"/> instance.
		/// </summary>
		/// <remarks>
		/// Implementations should not put any API keys and other credentials in the <see cref="HttpClient"/> instance.
		/// </remarks>
		/// <returns>The configured <see cref="HttpClient"/> instance.</returns>
		protected virtual HttpClient CreateHttpClient()
		{
			return new HttpClient
			{
				Timeout = Timeout.InfiniteTimeSpan
			};
		}
	}
}