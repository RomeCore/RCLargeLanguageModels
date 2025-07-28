namespace RCLargeLanguageModels.Messages
{
	/// <summary>
	/// Represents a message in chat with LLM that have sender information.
	/// </summary>
	public interface ISenderMessage : IMessage
	{
		/// <summary>
		/// Gets the sender identifier of the message. It can be name or something else. Used by some clients to identify senders of separate user messages.
		/// </summary>
		string? Sender { get; }
	}
}