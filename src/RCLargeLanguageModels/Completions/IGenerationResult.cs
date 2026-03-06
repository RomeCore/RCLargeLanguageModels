using System.Collections.Generic;
using RCLargeLanguageModels.Metadata;

namespace RCLargeLanguageModels.Completions
{
	/// <summary>
	/// Represents a generation result made by LLM.
	/// </summary>
	/// <remarks>
	/// May be chat completion, general completion or embedding result.
	/// </remarks>
	public interface IGenerationResult<T> : IMetadataProvider
	{
		/// <summary>
		/// The source client that used to generate this completion.
		/// </summary>
		LLMClient Client { get; }

		/// <summary>
		/// The source model descriptor that used to generate this completion.
		/// </summary>
		LLModelDescriptor Model { get; }

		/// <summary>
		/// Gets the list of available completion choices. Will contain at least one choice.
		/// </summary>
		IReadOnlyList<T> Choices { get; }

		/// <summary>
		/// Gets the first completion from the choices.
		/// </summary>
		T Completion { get; }

		/// <summary>
		/// Gets the collection of generation metadata (such as usage stats: <see cref="IUsageMetadata"/>).
		/// </summary>
		new IMetadataCollection Metadata { get; }

		/// <summary>
		/// Gets the usage metadata for this completion result. Can be <see langword="null"/>.
		/// </summary>
		IUsageMetadata? UsageMetadata { get; }
	}
}