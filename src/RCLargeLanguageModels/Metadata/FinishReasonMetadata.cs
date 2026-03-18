using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Metadata
{
	/// <summary>
	/// Represents a finish reason that caused completion to stop.
	/// </summary>
	public enum FinishReason
	{
		/// <summary>
		/// The finish reason is unknown or not specified.
		/// Used for unmapped API values or undefined cases.
		/// </summary>
		Unknown,

		/// <summary>
		/// The model hit a natural stop point or a provided stop sequence.
		/// Maps to API value: "stop"
		/// </summary>
		Stop,

		/// <summary>
		/// The maximum number of tokens specified in the request was reached.
		/// Maps to API value: "length"
		/// </summary>
		Length,

		/// <summary>
		/// Content was omitted due to content filter flags.
		/// Maps to API value: "content_filter"
		/// </summary>
		ContentFilter,

		/// <summary>
		/// The model called a tool/function.
		/// Maps to API value: "tool_calls" or "function_call"
		/// </summary>
		ToolCalls,

		/// <summary>
		/// The request was interrupted due to insufficient system resources.
		/// Maps to API value: "insufficient_system_resource"
		/// </summary>
		InsufficientResources
	}

	/// <summary>
	/// Represents a finish reason completion metadata that contains stop reason.
	/// </summary>
	public interface IFinishReasonMetadata : IChoiceCompletionMetadata
	{
		/// <summary>
		/// Gets the stop reason that caused completion to stop.
		/// </summary>
		public FinishReason FinishReason { get; }
	}

	/// <inheritdoc cref="IFinishReasonMetadata"/>
	public class FinishReasonMetadata : IFinishReasonMetadata
	{
		public FinishReason FinishReason { get; }

		/// <summary>
		/// Creates a new instance of <see cref="FinishReasonMetadata"/>
		/// </summary>
		public FinishReasonMetadata(FinishReason finishReason)
		{
			FinishReason = finishReason;
		}
	}
}