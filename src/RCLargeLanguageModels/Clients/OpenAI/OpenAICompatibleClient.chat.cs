using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Completions.Properties;
using RCLargeLanguageModels.Formats;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Metadata;
using RCLargeLanguageModels.Statistics;
using RCLargeLanguageModels.Tools;
using RCLargeLanguageModels.Utilities;
using Serilog;

namespace RCLargeLanguageModels.Clients.OpenAI
{
	public partial class OpenAICompatibleClient
	{
		protected class PartialMessageToolCallContext
		{
			public string ToolCallId { get; set; }
			public string ToolCallType { get; set; }

			public string ToolCallFunctionName { get; set; }
			public StringBuilder ToolCallFunctionArguments { get; }

			public PartialMessageToolCallContext()
			{
				ToolCallId = string.Empty;
				ToolCallType = string.Empty;

				ToolCallFunctionName = string.Empty;
				ToolCallFunctionArguments = new StringBuilder();
			}
		}


		protected override async Task<ChatCompletionResult> CreateChatCompletionsOverrideAsync(LLModelDescriptor model,
			List<IMessage> messages, int count, List<CompletionProperty> properties, OutputFormatDefinition outputFormatDefinition, ToolSet tools, CancellationToken cancellationToken)
		{
			var body = BuildChatRequestBody(model, messages, outputFormatDefinition, tools, properties, count, false);
			var headers = GetRequestHeaders();

			var response = await RequestUtility.GetResponseAsync(RequestType.Post, _endpoint.GenerateChatCompletion,
				body, _http, headers, cancellationToken);
			response.EnsureSuccessStatusCode();

			var responseContent = await response.ParseContentAsync<JsonObject>(cancellationToken);
			if (responseContent["choices"] is not JsonArray choices || choices.Count == 0)
				throw new InvalidDataException("No choices in response.");
			
			List<AssistantMessage> resultMessages = new List<AssistantMessage>();

			foreach (var choice in choices)
			{
				var message = choice!["message"] as JsonObject
					?? throw new InvalidDataException("No message in 'choice'.");

				var completionMetadata = new List<IMetadata>();
				if (choice["finish_reason"]?.GetValue<string>() is string finishReason)
					completionMetadata.Add(GetFinishReasonMetadata(finishReason));

				resultMessages.Add(ParseNonStreamingAssistantMessage(message, model, tools, completionMetadata));
			}

			var metadata = new List<IMetadata>();
			if (responseContent["usage"] is JsonObject usage)
				metadata.Add(GetUsageMetadata(usage));

			return new ChatCompletionResult(this, model, resultMessages, metadata);
		}

		protected override Task<PartialChatCompletionResult> CreateStreamingChatCompletionsOverrideAsync(LLModelDescriptor model,
			List<IMessage> messages, int count, List<CompletionProperty> properties, OutputFormatDefinition outputFormatDefinition, ToolSet tools, CancellationToken cancellationToken)
		{
			var resultMessages = Enumerable.Range(0, count).Select(i => new PartialAssistantMessage()).ToImmutableArray();
			var contexts = Enumerable.Range(0, count).Select(i => new List<PartialMessageToolCallContext?>()).ToImmutableArray();
			var result = new PartialChatCompletionResult(this, model, resultMessages);

			void OnDataReceived(JsonObject data)
			{
				if (data["choices"] is not JsonArray choices || choices.Count == 0)
					throw new InvalidDataException("No choices in response.");

				foreach (var choice in choices)
				{
					int index = choice!["index"]!.GetValue<int>();
					var message = resultMessages[index];

					var delta = choice["delta"] as JsonObject
						?? throw new InvalidDataException("No delta in 'choice'.");
					var context = contexts[index];

					ParseStreamingAssistantMessage(delta, message, context, tools);

					var completionMetadata = new List<IMetadata>();
					if (choice["finish_reason"]?.GetValue<string>() is string finishReason)
						completionMetadata.Add(GetFinishReasonMetadata(finishReason));

					if (completionMetadata.Count > 0)
						message.Complete(completionMetadata);
				}

				var metadata = new List<IMetadata>();
				if (data["usage"] is JsonObject usage)
					metadata.Add(GetUsageMetadata(usage));

				if (metadata.Count > 0)
					result.Complete(metadata);
			}

			var body = BuildChatRequestBody(model, messages, outputFormatDefinition, tools, properties, count, true);
			var headers = GetRequestHeaders();

			Task.Run(() => RequestUtility.ProcessStreamingJsonResponseAsync<JsonObject>(RequestType.Post, _endpoint.GenerateChatCompletion,
				body, OnDataReceived, _http, headers, cancellationToken), cancellationToken)
				.ContinueWith(t =>
				{
					foreach (var message in resultMessages)
					{
						if (message.CompletionToken.IsCompleted)
							continue;
						if (t.IsFaulted)
							message.Fail(t.Exception);
						else if (t.IsCanceled)
							message.Cancel();
					}
				}, cancellationToken);

			return Task.FromResult(result);
		}

		protected virtual JsonObject BuildTool(ITool tool)
		{
			return tool switch
			{
				FunctionTool functionTool => new JsonObject
				{
					["type"] = "function",
					["function"] = new JsonObject
					{
						["name"] = functionTool.Name,
						["description"] = functionTool.Description,
						["parameters"] = functionTool.ArgumentSchema.DeepClone()
					}
				},
				_ => throw new InvalidOperationException("Unknown tool type"),
			};
		}

		protected virtual JsonObject BuildToolCall(IToolCall toolCall)
		{
			return toolCall switch
			{
				FunctionToolCall functionCall => new JsonObject
				{
					["id"] = functionCall.Id,
					["type"] = "function",
					["function"] = new JsonObject
					{
						["name"] = functionCall.ToolName,
						["arguments"] = functionCall.Args.ToJsonString()
					}
				},
				_ => throw new InvalidOperationException("Unknown tool call type"),
			};
		}

		protected virtual JsonObject BuildMessage(IMessage message, bool isLast)
		{
			return message switch
			{
				ISystemMessage systemMessage => BuildSystemMessage(systemMessage, isLast),
				IUserMessage userMessage => BuildUserMessage(userMessage, isLast),
				IAssistantMessage assistantMessage => BuildAssistantMessage(assistantMessage, isLast),
				IToolMessage toolMessage => BuildToolMessage(toolMessage, isLast),
				_ => throw new InvalidOperationException("Unknown message type."),
			};
		}

		protected virtual JsonObject BuildSystemMessage(ISystemMessage message, bool isLast)
		{
			return new JsonObject
			{
				["role"] = "system",
				["content"] = message.Content
			};
		}

		protected virtual JsonObject BuildUserMessage(IUserMessage message, bool isLast)
		{
			return new JsonObject
			{
				["role"] = "user",
				["content"] = message.Content
			};
		}

		protected virtual JsonObject BuildAssistantMessage(IAssistantMessage message, bool isLast)
		{
			var res = new JsonObject
			{
				["role"] = "assistant",
				["content"] = message.Content
			};

			var toolCalls = new JsonArray(message.ToolCalls.Select(BuildToolCall).ToArray());
			if (toolCalls.Count > 0)
				res["tool_calls"] = toolCalls;

			return res;
		}

		protected virtual JsonObject BuildToolMessage(IToolMessage message, bool isLast)
		{
			return new JsonObject
			{
				["role"] = "tool",
				["tool_call_id"] = message.ToolCallId,
				["content"] = message.Content
			};
		}

		protected virtual void PopulateBodyWithProperties(JsonObject body, LLModelDescriptor model, OutputFormatDefinition outputFormatDefinition, IEnumerable<ITool> tools, IEnumerable<CompletionProperty> properties)
		{
			foreach (var property in properties)
			{
				var propertyName = property.Name;
				var value = property.RawValue;

				switch (property)
				{
					case TemperatureProperty tp:
						value = tp.ToRange(-2, 2);
						break;
					case StopSequencesProperty ssp:
						value = ssp.Value.ToArray();
						break;
				}

				body.Add(propertyName, JsonSerializer.SerializeToNode(value));
			}
		}

		protected virtual JsonObject BuildChatRequestBody(LLModelDescriptor model, IEnumerable<IMessage> _messages,
			OutputFormatDefinition outputFormatDefinition, IEnumerable<ITool> tools, IEnumerable<CompletionProperty> properties,
			int count, bool stream)
		{
			var messagesList = _messages.ToList();
			int c = 0, lastIndex = messagesList.Count - 1;
			var messages = new JsonArray(messagesList
				.Select(m => BuildMessage(m, c++ == lastIndex))
				.ToArray());

			var result = new JsonObject
			{
				["model"] = model.Name,
				["messages"] = messages,
				["stream"] = stream
			};

			if (count > 1)
				result["n"] = count;

			if (tools.Any())
				result["tools"] = new JsonArray(tools.Select(BuildTool).ToArray());

			PopulateBodyWithProperties(result, model, outputFormatDefinition, tools, properties);

			return result;
		}

		protected virtual IToolCall ParseToolCall(JsonObject toolCall, IEnumerable<ITool> tools)
		{
			var id = toolCall["id"]?.GetValue<string>()!;
			var type = toolCall["type"]?.GetValue<string>();

			switch (type)
			{
				case "function":

					var function = toolCall["function"]!.AsObject();

					var name = function["name"]?.GetValue<string>()
						?? throw new ArgumentException("Missing 'name' in function tool call");
					var args = function["arguments"]?.GetValue<string>()
						?? throw new ArgumentException("Missing 'arguments' in function tool call");

					var tool = tools.First(t => t.Name == name);
					var functionTool = tool as FunctionTool
						?? throw new ArgumentException($"Tool '{name}' not found in tool set");
					var parsedArgs = JsonNode.Parse(args)!;

					return new FunctionToolCall(id, name, parsedArgs);

				default:

					throw new InvalidOperationException("Unknown tool call type");
			}
		}

		protected virtual IToolCall? ParseStreamingToolCall(JsonObject toolCall, PartialMessageToolCallContext context, IEnumerable<ITool> tools)
		{
			var id = toolCall["id"]?.GetValue<string>();
			if (id != null)
				context.ToolCallId = id;

			var type = toolCall["type"]?.GetValue<string>();
			if (type != null)
				context.ToolCallType = type;

			var function = toolCall["function"] as JsonObject;
			if (function != null)
			{
				var name = function["name"]?.GetValue<string>();
				if (name != null)
					context.ToolCallFunctionName = name;

				var args = function["arguments"]?.GetValue<string>();
				if (args != null)
					context.ToolCallFunctionArguments.Append(args);

				if (!string.IsNullOrWhiteSpace(context.ToolCallFunctionName) &&
					context.ToolCallFunctionArguments.Length > 0)
				{
					try
					{
						var tool = tools.First(t => t.Name == context.ToolCallFunctionName);

						if (tool is FunctionTool functionTool)
						{
							// TODO: Track brackets count and parse when its become 0

							var parsedArgs = JsonNode.Parse(context.ToolCallFunctionArguments.ToString());
							return new FunctionToolCall(context.ToolCallId, context.ToolCallFunctionName, parsedArgs!);
						}
					}
					catch
					{
					}
				}
			}

			return null;
		}

		protected virtual AssistantMessage ParseNonStreamingAssistantMessage(JsonObject message,
			LLModelDescriptor model, IEnumerable<ITool> tools, IEnumerable<IMetadata> metadata)
		{
			var content = message["content"]?.GetValue<string>();
			var reasoningContent = message["reasoning_content"]?.GetValue<string>(); // The DeepSeek is using that currently, why OpenAI can't?

			var parsedToolCalls = !(message["tool_calls"] is JsonArray toolCalls)
				? new List<IToolCall>()
				: toolCalls.Select(c => ParseToolCall(c!.AsObject(), tools)).ToList();

			return new AssistantMessage(content, reasoningContent, parsedToolCalls, completionMetadata: metadata);
		}

		protected virtual void ParseStreamingAssistantMessage(JsonObject delta, PartialAssistantMessage message,
			List<PartialMessageToolCallContext?> contexts, IEnumerable<ITool> tools)
		{
			var content = delta["content"]?.GetValue<string>();
			var reasoningContent = delta["reasoning_content"]?.GetValue<string>();

			var toolCalls = delta["tool_calls"] as JsonArray;
			var parsedToolCalls = new List<IToolCall>();

			if (toolCalls != null)
			{
				foreach (var toolCall in toolCalls.Cast<JsonObject>())
				{
					var index = toolCall["index"]?.GetValue<int>() ?? 0;
					while (contexts.Count <= index)
						contexts.Add(new PartialMessageToolCallContext());

					var context = contexts[index];
					if (context == null)
						continue;

					var parsedToolCall = ParseStreamingToolCall(toolCall, context, tools);
					if (parsedToolCall != null)
					{
						parsedToolCalls.Add(parsedToolCall);
						contexts[index] = null;
					}
				}
			}

			message.Add(content, reasoningContent, parsedToolCalls.AsReadOnly());
		}

		protected virtual IFinishReasonMetadata GetFinishReasonMetadata(string reason)
		{
			var value = reason switch
			{
				"stop" => FinishReason.Stop,
				"length" => FinishReason.Length,
				"content_filter" => FinishReason.ContentFilter,
				"tool_calls" => FinishReason.ToolCalls,
				"insufficient_system_resource" => FinishReason.InsufficientResources,
				_ => FinishReason.Unknown
			};
			return new FinishReasonMetadata(value);
		}

		protected virtual IUsageMetadata GetUsageMetadata(JsonObject usage)
		{
			var promptTokens = usage["prompt_tokens"]?.GetValue<int>() ?? 0;
			var promptCHTokens = usage["prompt_cache_hit_tokens"]?.GetValue<int>() ?? 0;
			var promptCMTokens = usage["prompt_cache_miss_tokens"]?.GetValue<int>() ?? 0;
			var completionTokens = usage["completion_tokens"]?.GetValue<int>() ?? 0;

			if (promptCHTokens != 0 && promptCMTokens != 0)
				return new UsageCacheMetadata(promptCHTokens, promptCMTokens, completionTokens, 0);

			return new UsageMetadata(promptTokens, completionTokens);
		}
	}
}