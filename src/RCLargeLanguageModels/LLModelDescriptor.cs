using System;
using RCLargeLanguageModels.Formats;
using System.Text;
using RCLargeLanguageModels.Metadata;

namespace RCLargeLanguageModels
{
	/// <summary>
	/// Represents a large language model (LLM) descriptor that contains information about the model and its capabilities.
	/// </summary>
	public class LLModelDescriptor : IMetadataProvider
	{
		/// <summary>
		/// Gets the client associated with the model. Can be null if the model is not associated with a client.
		/// </summary>
		public LLMClient Client { get; }

		/// <summary>
		/// Gets the name of the model.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the display name of the model.
		/// </summary>
		public string DisplayName { get; }

		/// <summary>
		/// Gets the full name of the model, which is the concatenation of the client name and the model name, delimited by '$'.
		/// </summary>
		public string FullName => Client != null ? Client.Name + '$' + Name : Name;
		
		/// <summary>
		/// Gets the full display name of the model, which is the concatenation of the client name and the model name, delimited by '$'.
		/// </summary>
		public string FullDisplayName => Client != null ? Client.DisplayName + '$' + DisplayName : DisplayName;

		/// <summary>
		/// Gets a value indicating the capabilities of the model. <see cref="LLMCapabilities.Unknown"/> by default.
		/// </summary>
		/// <remarks>
		/// Can be <see cref="LLMCapabilities.Unknown"/> (0) if the capabilities are unknown.
		/// </remarks>
		public LLMCapabilities Capabilities { get; }

		/// <summary>
		/// Gets the max context window (or length) in tokens. Can be -1 if unknown.
		/// </summary>
		public int ContextLength { get; }

		/// <summary>
		/// Gets a set of natively supported output formats. <see cref="OutputFormatSupportSet.Text"/> by default.
		/// </summary>
		public OutputFormatSupportSet SupportedOutputFormats { get; }

		/// <summary>
		/// Gets the metadata collection associated with this model. Can contain various metadata, including model version, description, language support, etc.
		/// </summary>
		public MetadataCollection Metadata { get; }
		IMetadataCollection IMetadataProvider.Metadata => Metadata;

		/// <summary>
		/// Creates new instance of <see cref="LLModelDescriptor"/> class.
		/// </summary>
		/// <param name="client">The client associated with the model. Can be null if the model is not associated with a client.</param>
		/// <param name="name">The name identifier of the model.</param>
		/// <param name="displayName">The human-readable display name of the model.</param>
		/// <param name="contextLength">the max context length in tokens. Can be -1 if unknown.</param>
		/// <param name="capabilities">The capabilities of the model.</param>
		/// <param name="supportedOutputFormats">The natively supported output formats of the model.</param>
		/// <param name="metadata">The metadata collection associated with this model.</param>
		public LLModelDescriptor(
			LLMClient client,
			string name,
			string displayName = null,
			LLMCapabilities? capabilities = null,
			int contextLength = -1,
			OutputFormatSupportSet supportedOutputFormats = null,
			MetadataCollection metadata = null
		)
		{
			Client = client;
			Name = name ?? throw new ArgumentNullException(nameof(name));
			DisplayName = displayName ?? name;
			Capabilities = capabilities ?? LLMCapabilities.Unknown;
			ContextLength = Math.Max(contextLength, -1);
			SupportedOutputFormats = supportedOutputFormats ?? OutputFormatSupportSet.Text;
			Metadata = metadata ?? MetadataCollection.Empty;
		}

		/// <summary>
		/// Creates new instance of <see cref="LLModelDescriptor"/> class.
		/// </summary>
		/// <param name="name">The name identifier of the model.</param>
		/// <param name="displayName">The human-readable display name of the model.</param>
		/// <param name="capabilities">The capabilities of the model.</param>
		/// <param name="contextLength">the max context length in tokens. Can be -1 if unknown.</param>
		/// <param name="supportedOutputFormats">The natively supported output formats of the model.</param>
		/// <param name="metadata">The metadata collection associated with this model.</param>
		public LLModelDescriptor(
			string name,
			string displayName = null,
			LLMCapabilities? capabilities = null,
			int contextLength = -1,
			OutputFormatSupportSet supportedOutputFormats = null,
			MetadataCollection metadata = null
		) : this (
			null,
			name,
			displayName,
			capabilities,
			contextLength,
			supportedOutputFormats,
			metadata
		)
		{
		}

		/// <summary>
		/// Creates a new <see cref="LLModelDescriptor"/> instance with the specified client while preserving all other properties.
		/// </summary>
		/// <param name="client">The LLM client to associate with the new descriptor.</param>
		/// <returns>A new <see cref="LLModelDescriptor"/> instance with updated client reference.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the client parameter is null.</exception>
		public LLModelDescriptor WithClient(LLMClient client)
		{
			return new LLModelDescriptor(
				client ?? throw new ArgumentNullException(nameof(client)),
				Name,
				DisplayName,
				Capabilities,
				ContextLength,
				SupportedOutputFormats);
		}

		/// <summary>
		/// Creates a new <see cref="LLModelDescriptor"/> instance without any associated client while preserving all other properties.
		/// </summary>
		/// <returns>A new <see cref="LLModelDescriptor"/> instance with no client association.</returns>
		public LLModelDescriptor WithoutClient()
		{
			return new LLModelDescriptor(
				Name,
				DisplayName,
				Capabilities,
				ContextLength,
				SupportedOutputFormats);
		}

		public override bool Equals(object obj)
		{
			if (obj is LLModelDescriptor other)
			{
				return Client == other.Client &&
					Name == other.Name &&
					DisplayName == other.DisplayName &&
					Capabilities == other.Capabilities &&
					SupportedOutputFormats == other.SupportedOutputFormats;
			}
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + (Client?.GetHashCode() ?? 0);
				hash = hash * 23 + Name.GetHashCode();
				hash = hash * 23 + DisplayName.GetHashCode();
				hash = hash * 23 + Capabilities.GetHashCode();
				hash = hash * 23 + ContextLength.GetHashCode();
				hash = hash * 23 + SupportedOutputFormats.GetHashCode();
				return hash;
			}
		}

		public static bool operator ==(LLModelDescriptor left, LLModelDescriptor right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(LLModelDescriptor left, LLModelDescriptor right)
		{
			return !Equals(left, right);
		}

		public override string ToString()
		{
			StringBuilder result = new StringBuilder();

			string clientName = Client?.DisplayName ?? "None";

			if (DisplayName == Name)
				result.AppendLine($"{Name}, client: {clientName}");
			else
				result.AppendLine($"{DisplayName} ({Name}), client: {clientName}");

			result.AppendLine($"Capabilities: {Capabilities}");

			if (SupportedOutputFormats.Count == 0)
				result.AppendLine("Supported output formats: None");
			else
				result.AppendLine($"Supported output formats: {string.Join(", ", SupportedOutputFormats)}");

			return result.ToString();
		}

		public static implicit operator LLModelDescriptor(string name)
		{
			return new LLModelDescriptor(name, capabilities: LLMCapabilities.Unknown);
		}
	}
}