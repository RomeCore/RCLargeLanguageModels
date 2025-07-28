using System;
using System.Collections.Generic;
using System.Text;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Messages;

namespace RCLargeLanguageModels.Metadata
{
	/// <summary>
	/// The marker interface for chat and general completions metadata, such as usage statistics
	/// </summary>
	public interface ICompletionMetadata : IMetadata
	{
	}

	/// <summary>
	/// The marker interface for <see cref="ICompletion"/> and <see cref="IAssistantMessage"/> completion metadata, such as stop reason.
	/// </summary>
	public interface IChoiceCompletionMetadata : IMetadata
	{
	}

	/// <summary>
	/// The marker interface for <see cref="ICompletion"/> and <see cref="IAssistantMessage"/> partial completion metadata, such as token logprobs.
	/// </summary>
	public interface IPartialCompletionMetadata : IMetadata
	{
	}
}