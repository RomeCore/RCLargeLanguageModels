using Newtonsoft.Json;

namespace RCLargeLanguageModels.Messages
{
	/// <summary>
	/// Represents a system message in chat with LLM.
	/// </summary>
	public class SystemMessage : ISystemMessage
	{
		[JsonIgnore]
		public Role Role => Role.System;

		/// <summary>
		/// The system instructions or context for the LLM.
		/// </summary>
		[JsonProperty]
		public string Content { get; }

		/// <summary>
		/// Creates a new instance of <see cref="SystemMessage"/>.
		/// </summary>
		/// <param name="content">The system message content.</param>
		[JsonConstructor]
		public SystemMessage(string content)
		{
			Content = content;
		}
	}
}