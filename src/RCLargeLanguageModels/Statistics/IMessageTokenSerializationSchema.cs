using System;
using System.Collections.Generic;
using System.Text;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tools;

namespace RCLargeLanguageModels.Statistics
{
	/// <summary>
	/// The serialization schema used for serializing messages to text that will be useful for counting tokens.
	/// </summary>
	public interface IMessageTokenSerializationSchema
	{
		/// <summary>
		/// Serializes a message for token counting.
		/// </summary>
		/// <param name="message">The message to serialize.</param>
		/// <param name="availableTools">The available tools that model can use, should be applicable when message is <see cref="ISystemMessage"/>.</param>
		/// <returns>The text to be used as input for token counter.</returns>
		string SerializeMessage(IMessage message, IEnumerable<ITool> availableTools);
	}
}