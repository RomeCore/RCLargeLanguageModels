namespace RCLargeLanguageModels.Completions.Properties
{
	/// <summary>
	/// Represents the level of reasoning effort for a reasoning model.
	/// </summary>
	public enum ReasoningEffort
	{
		/// <summary>
		/// Selects the default level of reasoning effort.
		/// </summary>
		Default,

		/// <summary>
		/// Absent or negligible reasoning effort.
		/// </summary>
		None,

		/// <summary>
		/// The reasoning model requires minimal effort to produce a response.
		/// </summary>
		Minimal,

		/// <summary>
		/// The reasoning model requires low effort to produce a response.
		/// </summary>
		Low,

		/// <summary>
		/// The reasoning model requires moderate effort to produce a response.
		/// </summary>
		Medium,

		/// <summary>
		/// The reasoning model requires high effort to produce a response.
		/// </summary>
		High,

		/// <summary>
		/// The reasoning model requires extra high effort to produce a response.
		/// </summary>
		XHigh,

		/// <summary>
		/// The reasoning model requires maximum effort to produce a response.
		/// </summary>
		Max
	}
}