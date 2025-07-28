using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Metadata
{
	/// <summary>
	/// Represents a stop reason that caused completion to stop.
	/// </summary>
	public enum StopReason
	{
		/// <summary>
		/// The stop reason is unknown or not specified.
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
	/// Represents a stop reason completion metadata that contains stop reason.
	/// </summary>
	public interface IStopReasonMetadata : IChoiceCompletionMetadata
	{
		/// <summary>
		/// Gets the stop reason that caused completion to stop.
		/// </summary>
		public StopReason StopReason { get; }
	}

	/// <inheritdoc cref="IStopReasonMetadata"/>
	public class StopReasonMetadata : IStopReasonMetadata
	{
		public StopReason StopReason { get; }

		/// <summary>
		/// Creates a new instance of <see cref="StopReasonMetadata"/>
		/// </summary>
		public StopReasonMetadata(StopReason stopReason)
		{
			StopReason = stopReason;
		}
	}
}