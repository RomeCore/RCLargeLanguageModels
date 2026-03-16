using System.Collections.Generic;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tools;

namespace RCLargeLanguageModels.Statistics
{
	/// <summary>
	/// The serialization schema used for serializing messages to text that will be useful for counting tokens.
	/// </summary>
	public abstract class MessageTokenSerializationSchema : IMessageTokenSerializationSchema
	{
		public abstract string SerializeMessage(IMessage message, IEnumerable<ITool> availableTools);

		/// <summary>
		/// Gets the default instance of <see cref="MessageTokenSerializationSchema"/>.
		/// </summary>
		public static MessageTokenSerializationSchema Default { get; } = new OpenAIMessageTokenSerializationSchema();
	}
}