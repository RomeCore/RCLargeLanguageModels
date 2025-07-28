using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Clients;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Formats;
using RCLargeLanguageModels.Security;
using RCLargeLanguageModels.Tools;
using Serilog;
using System.Diagnostics.Metrics;
using RCLargeLanguageModels.Metadata;

namespace RCLargeLanguageModels
{
	/// <summary>
	/// Marks an attribute to be registered in the <see cref="LLMClientRegistry"/>.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class LLMClientAttribute : Reflector.DefineBaseAttribute
	{
	}

	/// <summary>
	/// Marks a constructor that should be used to create a new instance of the <see cref="LLMClient"/> class in the <see cref="LLMClientRegistry"/>.
	/// </summary>
	/// <remarks>
	/// All parameters of the constructor must be marked with the <see cref="LLMAPIKeyAttribute"/> attribute and be of type <see cref="string"/> or <see cref="ITokenAccessor"/>.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false)]
	public class LLMClientConstructorAttribute : Reflector.DefineBaseAttribute
	{
	}

	/// <summary>
	/// Marks a constructor parameter to be injected by the <see cref="LLMClientRegistry"/> to retrieve the API key.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	public class LLMAPIKeyAttribute : Reflector.DefineBaseAttribute
	{
		/// <summary>
		/// Gets the API key ID associated with the attribute.
		/// </summary>
		public string ApiKeyId { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LLMAPIKeyAttribute"/> class with the specified API key ID.
		/// </summary>
		/// <param name="apiKeyId">The API key ID.</param>
		public LLMAPIKeyAttribute(string apiKeyId)
		{
			ApiKeyId = apiKeyId;
		}
	}

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