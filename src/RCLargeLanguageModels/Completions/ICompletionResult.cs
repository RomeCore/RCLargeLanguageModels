using System;
using System.Collections.Generic;
using RCLargeLanguageModels.Metadata;

namespace RCLargeLanguageModels.Completions
{
	/// <summary>
	/// Represents a LLM-generated completion result.
	/// </summary>
	public interface ICompletionResult : IMetadataProvider
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
		IReadOnlyList<ICompletion> Choices { get; }

		/// <summary>
		/// Gets the first completion from the choices.
		/// </summary>
		ICompletion Completion { get; }

		/// <summary>
		/// Gets the content of the first completion choice.
		/// </summary>
		string? Content { get; }

		/// <summary>
		/// Gets the collection of completion metadata (such as usage stats: <see cref="IUsageMetadata"/>).
		/// </summary>
		new IMetadataCollection Metadata { get; }

		/// <summary>
		/// Gets the usage metadata for this completion result. Can be <see langword="null"/>.
		/// </summary>
		IUsageMetadata? UsageMetadata { get; }
	}
}