using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RCLargeLanguageModels.Messages.Attachments;

namespace RCLargeLanguageModels.Messages
{
	/// <summary>
	/// Represents a user message in chat with LLM.
	/// </summary>
	public class UserMessage : IUserMessage
	{
		public Role Role => Role.User;

		/// <summary>
		/// Gets the sender identifier of the user message.
		/// </summary>
		public string Sender { get; }

		/// <summary>
		/// The user message content.
		/// </summary>
		public string Content { get; }

		/// <summary>
		/// Gets the attachments list of the user message.
		/// </summary>
		public IReadOnlyList<IAttachment> Attachments { get; }

		/// <summary>
		/// Gets the value indicating whether the user message is empty.
		/// </summary>
		public bool IsEmpty => string.IsNullOrEmpty(Content) && !Attachments.Any();

		/// <summary>
		/// Creates a new instance of <see cref="UserMessage"/> class.
		/// </summary>
		/// <param name="content">The content of the user message.</param>
		public UserMessage(string content)
		{
			Sender = Senders.User;
			Content = content ?? throw new ArgumentNullException(nameof(content));
			Attachments = Array.Empty<IAttachment>();
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="UserMessage"/> class.
		/// </summary>
		/// <param name="sender">The sender identifier of this message.</param>
		/// <param name="content">The content of the user message.</param>
		public UserMessage(string sender, string content)
		{
			Sender = sender ?? throw new ArgumentNullException(nameof(sender));
			Content = content ?? throw new ArgumentNullException(nameof(content));
			Attachments = Array.Empty<IAttachment>();
		}

		/// <summary>
		/// Creates a new instance of <see cref="UserMessage"/> class with the specified attachments.
		/// </summary>
		/// <param name="sender">The sender identifier of this message.</param>
		/// <param name="content">The content of the user message.</param>
		/// <param name="attachments">The attachments of the message.</param>
		[JsonConstructor]
		public UserMessage(string sender, string content, IEnumerable<IAttachment> attachments)
		{
			Sender = sender ?? throw new ArgumentNullException(nameof(sender));
			Content = content ?? throw new ArgumentNullException(nameof(content));
			Attachments = attachments?.ToList().AsReadOnly() ?? throw new ArgumentNullException(nameof(attachments));
		}
	}
}