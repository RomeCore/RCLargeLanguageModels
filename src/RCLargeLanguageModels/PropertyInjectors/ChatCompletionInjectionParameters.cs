using System.Collections.Generic;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Formats;
using RCLargeLanguageModels.Tools;
using RCLargeLanguageModels.Completions;

namespace RCLargeLanguageModels.PropertyInjectors
{
	/// <summary>
	/// Represents the parameters for injecting in the chat completions.
	/// </summary>
	public class ChatCompletionInjectionParameters
	{
		/// <summary>
		/// The model that uses injector.
		/// </summary>
		public LLModel Model { get; }

		/// <summary>
		/// The list of messages to complete.
		/// </summary>
		public List<IMessage> Messages { get; set; }

		/// <summary>
		/// The number of completions to create.
		/// </summary>
		public int Count { get; set; } = 1;

		/// <summary>
		/// The completion properties. For example: temperature, top_p, etc.
		/// </summary>
		public List<CompletionProperty> Properties { get; set; }

		/// <summary>
		/// The tools to use in the completion.
		/// </summary>
		public ToolSet Tools { get; set; }

		/// <summary>
		/// The output format definition.
		/// </summary>
		public OutputFormatDefinition OutputFormatDefinition { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatCompletionInjectionParameters"/> class.
		/// </summary>
		public ChatCompletionInjectionParameters(LLModel model, List<IMessage> messages, int count = 1, List<CompletionProperty> properties = null, ToolSet tools = null, OutputFormatDefinition outputFormatDefinition = null)
		{
			Model = model;
			Messages = messages;
			Count = count;
			Properties = properties ?? new List<CompletionProperty>();
			Tools = tools ?? new ToolSet();
			OutputFormatDefinition = outputFormatDefinition ?? OutputFormatDefinition.Empty;
		}
	}
}