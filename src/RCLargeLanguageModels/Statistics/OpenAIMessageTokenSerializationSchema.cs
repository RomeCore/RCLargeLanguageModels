using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tools;

namespace RCLargeLanguageModels.Statistics
{
	public class OpenAIMessageTokenSerializationSchema : MessageTokenSerializationSchema
	{
		private static readonly JsonSerializerOptions _jsonOpts = new()
		{
			// WriteIndented = true,
			Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
		};

		public override string SerializeMessage(IMessage message, IEnumerable<ITool> availableTools)
		{
			return message switch
			{
				ISystemMessage sysMsg => SerializeSystemMessage(sysMsg, availableTools),
				IUserMessage usrMsg => SerializeUserMessage(usrMsg),
				IAssistantMessage astMsg => SerializeAssistantMessage(astMsg),
				IToolMessage tolMsg => SerializeToolMessage(tolMsg),
				_ => message.Content
			};
		}

		private static string SerializeSystemMessage(ISystemMessage message, IEnumerable<ITool> availableTools)
		{
			StringBuilder sb = new(message.Content);

			foreach (var tool in availableTools)
			{
				if (sb.Length > 0)
					sb.Append("\n\n");

				switch (tool)
				{
					case FunctionTool function:

						sb.AppendLine(
$@"<tool name={function.Name}>
{{
	""description"": ""{function.Description}"",
	""type"": ""function"",
	""parameters"": {function.ArgumentSchema.ToJsonString(_jsonOpts)}
}}
</tool>");

						break;
				}
			}

			return sb.ToString();
		}

		private static string SerializeUserMessage(IUserMessage message)
		{
			return message.Content;
		}

		private static string SerializeAssistantMessage(IAssistantMessage message)
		{
			StringBuilder sb = new(message.Content);

			foreach (var toolCall in message.ToolCalls)
			{
				switch (toolCall)
				{
					case FunctionToolCall functionCall:
						sb.AppendLine(
$@"<function name={toolCall.ToolName} id=""{functionCall.Id}"">
{functionCall.Args.ToJsonString(_jsonOpts)}
</function>");
						break;
				}
			}

			return sb.ToString();
		}

		private static string SerializeToolMessage(IToolMessage message)
		{
			return message.Content;
		}
	}
}
