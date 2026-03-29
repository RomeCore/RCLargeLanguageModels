namespace RCLargeLanguageModels.Tasks
{
	/// <summary>
	/// Represents a completion state.
	/// </summary>
	public enum CompletionState
	{
		/// <summary>
		/// The incomplete state.
		/// </summary>
		Incomplete,

		/// <summary>
		/// The success state.
		/// </summary>
		Success,

		/// <summary>
		/// The cancelled state.
		/// </summary>
		Cancelled,

		/// <summary>
		/// The failed state.
		/// </summary>
		Failed
	}
}