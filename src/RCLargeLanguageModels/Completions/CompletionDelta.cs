using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using RCLargeLanguageModels.Metadata;

namespace RCLargeLanguageModels.Completions
{
	/// <summary>
	/// Represents a general completion delta.
	/// </summary>
	public class CompletionDelta
	{
		/// <summary>
		/// Gets the delta content associated with this completion delta. Can be <see langword="null"/>.
		/// </summary>
		public string? DeltaContent { get; }

		/// <summary>
		/// The new partial metadata for the completion. Can be <see langword="null"/>.
		/// </summary>
		public ImmutableList<IMetadata>? NewPartialMetadata { get; }

		/// <summary>
		/// Gets the value indicating whether this delta can be condered empty.
		/// </summary>
		public bool IsEmpty =>
			string.IsNullOrEmpty(DeltaContent) &&
			NewPartialMetadata.IsNullOrEmpty();

		/// <summary>
		/// Initializes a new instance of <see cref="CompletionDelta"/> class.
		/// </summary>
		/// <param name="deltaContent"></param>
		public CompletionDelta(string? deltaContent)
		{
			DeltaContent = deltaContent;
			NewPartialMetadata = null;
		}

		/// <summary>
		/// Initializes a new instance of <see cref="CompletionDelta"/> class.
		/// </summary>
		/// <param name="deltaContent"></param>
		/// <param name="newPartialMetadata"></param>
		public CompletionDelta(
			string? deltaContent = null,
			IEnumerable<IMetadata>? newPartialMetadata = null)
		{
			DeltaContent = deltaContent;
			NewPartialMetadata = newPartialMetadata?.ToImmutableList();
		}
	}
}