using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Metadata
{
	/// <summary>
	/// Marker interface for LLM model-specific metadata.
	/// </summary>
	public interface IModelMetadata : IMetadata
	{
	}

	/// <summary>
	/// Marker interface for LLM client-specific metadata.
	/// </summary>
	public interface IClientMetadata : IMetadata
	{
	}
}