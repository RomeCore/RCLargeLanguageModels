namespace RCLargeLanguageModels.Messages
{
	/// <summary>
	/// Represents a role of message in chat with LLM.
	/// </summary>
	public enum Role
	{
		/// <summary>
		/// A system message that sets the context or instructs the LLM.
		/// </summary>
		System,

		/// <summary>
		/// The user message that have text message content along with optional attachments.
		/// </summary>
		User,

		/// <summary>
		/// The completed or partial (for streaming responses) assitant message that contains the AI's response and optional tool calls.
		/// </summary>
		/// <remarks>
		/// <see cref="PartialAssistantMessage"/> сan be converted back into <see cref="AssistantMessage"/> message after completion.
		/// </remarks>
		Assistant,

		/// <summary>
		/// The tool message that contains the response from a tool call. May have attachments.
		/// </summary>
		Tool
	}
}