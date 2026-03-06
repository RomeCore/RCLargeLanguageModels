using System;
using System.Collections.Generic;
using RCLargeLanguageModels.Metadata;

namespace RCLargeLanguageModels.Completions
{
	/// <summary>
	/// Represents a LLM-generated completion result.
	/// </summary>
	public interface ICompletionResult : IGenerationResult<ICompletion>, IContentHolder
	{
	}
}