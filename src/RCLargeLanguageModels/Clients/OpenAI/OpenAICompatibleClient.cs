using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema.Generation;
using RCLargeLanguageModels.Json;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Formats;
using RCLargeLanguageModels.Security;
using RCLargeLanguageModels.Statistics;
using RCLargeLanguageModels.Tasks;
using RCLargeLanguageModels.Tools;
using RCLargeLanguageModels.Utilities;
using Serilog;
using RCLargeLanguageModels.Completions;
using System.Net.Http;
using System.Collections.Immutable;
using RCLargeLanguageModels.Metadata;
using System.Runtime.InteropServices.ComTypes;

namespace RCLargeLanguageModels.Clients.OpenAI
{
	public class OpenAIEndpointConfig : LLMEndpointConfig
	{
		public OpenAIEndpointConfig(string baseUri) : base(baseUri)
		{
		}

		public override string GenerateChatCompletion => BaseUri + "/chat/completions";
		public override string GenerateCompletion => BaseUri + "/completions";
		public override string ListModels => BaseUri + "/models";
	}

	/// <summary>
	/// Represents a client for interacting with the OpenAI-compatible API.
	/// </summary>
	/// <remarks>
	/// Mostly based on DeepSeek documentation, since OpenAI API is not yet available.
	/// </remarks>
	public class OpenAICompatibleClient : LLMClient
	{
		protected class PartialMessageToolCallContext
		{
			public string ToolCallId { get; set; }
			public string ToolCallType { get; set; }

			public string ToolCallFunctionName { get; set; }
			public StringBuilder ToolCallFunctionArguments { get; }

			public PartialMessageToolCallContext()
			{
				ToolCallFunctionArguments = new StringBuilder();
			}
		}

		private readonly HttpClient _http;
		private readonly LLMEndpointConfig _endpoint;
		private readonly ITokenAccessor _apiKeyAccessor;

		public override string Name => "openai-compatible";
		public override string DisplayName => "OpenAI Compatible";

		/// <summary>
		/// Creates a new instance of the OpenAI-compatible client using the specified base URI and API key.
		/// </summary>
		/// <param name="baseUri">The base URI of the OpenAI-compatible API.</param>
		/// <param name="apiKey">The API key for authentication.</param>
		public OpenAICompatibleClient(string baseUri, string apiKey)
		{
			_apiKeyAccessor = new StringTokenAccessor(apiKey);
			_http = CreateHttpClient();
			_endpoint = new OpenAIEndpointConfig(baseUri ?? throw new ArgumentNullException(nameof(baseUri)));
		}

		/// <summary>
		/// Creates a new instance of the OpenAI-compatible client using the specified base URI and API key accessor.
		/// </summary>
		/// <param name="baseUri">The base URI of the OpenAI-compatible API.</param>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		public OpenAICompatibleClient(string baseUri, ITokenAccessor tokenAccessor)
		{
			_apiKeyAccessor = tokenAccessor ?? throw new ArgumentNullException(nameof(tokenAccessor));
			_http = CreateHttpClient();
			_endpoint = new OpenAIEndpointConfig(baseUri ?? throw new ArgumentNullException(nameof(baseUri)));
		}

		/// <summary>
		/// Creates a new instance of the OpenAI-compatible client using the specified endpoint configuration and API key.
		/// </summary>
		/// <param name="endpointConfig">The endpoint configuration for the OpenAI-compatible API.</param>
		/// <param name="apiKey">The API key for authentication.</param>
		public OpenAICompatibleClient(LLMEndpointConfig endpointConfig, string apiKey)
		{
			_apiKeyAccessor = new StringTokenAccessor(apiKey);
			_http = CreateHttpClient();
			_endpoint = endpointConfig ?? throw new ArgumentNullException(nameof(endpointConfig));
		}

		/// <summary>
		/// Creates a new instance of the OpenAI-compatible client using the specified endpoint configuration and API key accessor.
		/// </summary>
		/// <param name="endpointConfig">The endpoint configuration for the OpenAI-compatible API.</param>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		public OpenAICompatibleClient(LLMEndpointConfig endpointConfig, ITokenAccessor tokenAccessor)
		{
			_apiKeyAccessor = tokenAccessor ?? throw new ArgumentNullException(nameof(tokenAccessor));
			_http = CreateHttpClient();
			_endpoint = endpointConfig ?? throw new ArgumentNullException(nameof(endpointConfig));
		}

		protected virtual Dictionary<string, string> GetRequestHeaders()
		{
			return new Dictionary<string, string>
			{
				{ "Authorization", "Bearer " + _apiKeyAccessor.GetToken() }
			};
		}

		protected override async Task<LLModelDescriptor[]> ListModelsOverrideAsync(CancellationToken cancellationToken = default)
		{
			var headers = GetRequestHeaders();

			var response = await RequestUtility.GetResponseAsync<JObject>(RequestType.Get, _endpoint.ListModels,
				null, _http, headers, cancellationToken);

			var models = response["data"] as JArray;
			if (models == null)
				throw new Exception("No models in response.");

			var result = new List<LLModelDescriptor>();
			foreach (var model in models)
			{
				var id = model["id"]?.Value<string>();
				result.Add(new LLModelDescriptor(this, id));
			}

			return result.ToArray();
		}

		public override ICompletionProperties ConvertOrCreateCompletionProperties(ICompletionProperties chatProperties)
		{
			if (chatProperties == null)
				return new CompletionProperties();

			if (chatProperties is CompletionProperties)
				return chatProperties;

			return new CompletionProperties
			{
				MaxTokens = chatProperties.MaxTokens,
				Temperature = chatProperties.Temperature,
				TopP = chatProperties.TopP,
				Stop = chatProperties.Stop
			};
		}

		protected override async Task<ChatCompletionResult> CreateChatCompletionsOverrideAsync(LLModelDescriptor model, IEnumerable<IMessage> messages, int count, ICompletionProperties properties, OutputFormatDefinition outputFormatDefinition, IEnumerable<ITool> tools, CancellationToken cancellationToken)
		{
			var body = BuildChatRequestBody(model, messages, outputFormatDefinition, tools, properties, count, false);
			var headers = GetRequestHeaders();

			var response = await RequestUtility.GetResponseAsync<JObject>(RequestType.Post, _endpoint.GenerateChatCompletion,
				body, _http, headers, cancellationToken);

			var error = response["error"] as JObject ?? response;
			var errorCode = error["code"]?.Value<string>();
			if (errorCode != null)
				throw new InvalidDataException($"Error {errorCode}: {error["message"]}"); // TODO: Change to custom exception

			var choices = response["choices"] as JArray;
			if (choices == null || choices.Count == 0)
				throw new InvalidDataException("No choices in response.");

			List<AssistantMessage> resultMessages = new List<AssistantMessage>();

			foreach (JObject choice in choices)
			{
				var message = choice["message"] as JObject
					?? throw new InvalidDataException("No message in 'choice'.");

				resultMessages.Add(ParseNonStreamingAssistantMessage(message, model, tools));
			}

			var usage = response["usage"] as JObject;
			if (usage != null)
				AppendUsage(usage, model);

			return new ChatCompletionResult(this, model, resultMessages);
		}

		protected override Task<PartialChatCompletionResult> CreateStreamingChatCompletionsOverrideAsync(LLModelDescriptor model, IEnumerable<IMessage> messages, int count, ICompletionProperties properties, OutputFormatDefinition outputFormatDefinition, IEnumerable<ITool> tools, CancellationToken cancellationToken)
		{
			var resultMessages = Enumerable.Range(0, count).Select(i => new PartialAssistantMessage()).ToImmutableArray();
			var contexts = Enumerable.Range(0, count).Select(i => new List<PartialMessageToolCallContext>()).ToImmutableArray();

			try
			{
				var body = BuildChatRequestBody(model, messages, outputFormatDefinition, tools, properties, count, true);
				var headers = GetRequestHeaders();

				string bodyStr = body.ToString();

				Task.Run(() => RequestUtility.ProcessStreamingJsonResponseAsync<JObject>(RequestType.Post, _endpoint.GenerateChatCompletion,
					body, data =>
					{
						var choices = data["choices"] as JArray;
						if (choices == null || choices.Count == 0)
							throw new InvalidDataException("No choices in response.");

						foreach (JObject choice in choices)
						{
							int index = choice["index"].Value<int>();
							var message = resultMessages[index];

							string? finishReason = choice["finish_reason"]?.Value<string>();
							if (finishReason != null)
							{
								message.Complete();
							}

							var delta = choice["delta"] as JObject
								?? throw new InvalidDataException("No delta in 'choice'.");
							var context = contexts[index];

							ParseStreamingAssistantMessage(delta, message, context, tools);
						}

						var usage = data["usage"] as JObject;
						if (usage != null)
							AppendUsage(usage, model);
					}, _http, headers, cancellationToken))
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
					}, TaskScheduler.Default);

			}
			catch (Exception exc)
			{
				Log.Error(exc, "Error in streaming request");
			}

			return Task.FromResult(new PartialChatCompletionResult(this, model, resultMessages));
		}

		protected virtual JObject BuildTool(ITool tool)
		{
			switch (tool)
			{
				case FunctionTool functionTool:
					return new JObject
					{
						["type"] = "function",
						["function"] = new JObject
						{
							["name"] = functionTool.Name,
							["description"] = functionTool.Description,
							["parameters"] = functionTool.ArgumentSchema.ToJToken()
						}
					};

				default:
					throw new InvalidOperationException("Unknown tool type");
			}
		}

		protected virtual JObject BuildToolCall(IToolCall toolCall)
		{
			switch (toolCall)
			{
				case FunctionToolCall functionCall:

					return new JObject
					{
						["id"] = functionCall.Id,
						["type"] = "function",
						["function"] = new JObject
						{
							["name"] = functionCall.ToolName,
							["arguments"] = JsonConvert.SerializeObject(functionCall.Args)
						}
					};

				default:
					throw new InvalidOperationException("Unknown tool call type");
			}
		}

		protected virtual JObject BuildMessage(IMessage message, bool isLast)
		{
			switch (message)
			{
				case SystemMessage systemMessage:
					return new JObject
					{
						["role"] = "system",
						["content"] = systemMessage.Content
					};

				case UserMessage userMessage:
					return new JObject
					{
						["role"] = "user",
						["content"] = userMessage.BuildContentWithTextAttachments()
					};

				case IAssistantMessage assistantMessage:
					var res = new JObject
					{
						["role"] = "assistant",
						["content"] = assistantMessage.BuildContentWithTextAttachments()
					};

					var toolCalls = new JArray(assistantMessage.ToolCalls.Select(BuildToolCall));
					if (toolCalls.Count > 0)
						res["tool_calls"] = toolCalls;

					return res;

				case ToolMessage toolMessage:
					return new JObject
					{
						["role"] = "tool",
						["tool_call_id"] = toolMessage.ToolCallId,
						["content"] = toolMessage.BuildContentWithTextAttachments()
					};

				default:
					throw new InvalidOperationException("Unknown message type.");
			}
		}

		protected virtual void PopulateBodyWithProperties(JObject body, LLModelDescriptor model, OutputFormatDefinition outputFormatDefinition, IEnumerable<ITool> tools, ICompletionProperties properties)
		{
			if (properties != null)
			{
				body.AddIfNotNull("max_tokens", properties.MaxTokens);
				body.AddIfNotNull("stop", properties.Stop?.ToArray());
				body.AddIfNotNull("temperature", properties.Temperature);
				body.AddIfNotNull("top_p", properties.TopP);
			}
		}

		protected virtual JObject BuildChatRequestBody(LLModelDescriptor model, IEnumerable<IMessage> _messages,
			OutputFormatDefinition outputFormatDefinition, IEnumerable<ITool> tools, ICompletionProperties properties,
			int count, bool stream)
		{
			var messages = _messages.ToList();
			var builtMessages = new List<JObject>(messages.Count);
			int c = 0, lastIndex = messages.Count - 1;

			foreach (var message in messages)
			{
				builtMessages.Add(BuildMessage(message, c == lastIndex));
				c++;
			}

			var result = new JObject
			{
				["model"] = model.Name,
				["messages"] = new JArray(builtMessages),
				["n"] = count,
				["stream"] = stream
			};

			if (tools.Any())
			{
				result["tools"] = new JArray(tools.Select(BuildTool));
			}

			PopulateBodyWithProperties(result, model, outputFormatDefinition, tools, properties);

			return result;
		}

		protected virtual IToolCall ParseToolCall(JObject toolCall, IEnumerable<ITool> tools)
		{
			var id = toolCall["id"]?.Value<string>();
			var type = toolCall["type"]?.Value<string>();

			switch (type)
			{
				case "function":

					var function = toolCall["function"] as JObject;

					var name = function["name"]?.Value<string>()
						?? throw new ArgumentException("Missing 'name' in function tool call");
					var args = function["arguments"]?.Value<string>()
						?? throw new ArgumentException("Missing 'arguments' in function tool call");

					var tool = tools.First(t => t.Name == name);
					var functionTool = tool as FunctionTool
						?? throw new ArgumentException($"Tool '{name}' not found in tool set");
					var parsedArgs = JToken.Parse(args);

					return new FunctionToolCall(id, name, parsedArgs);

				default:

					throw new InvalidOperationException("Unknown tool call type");
			}
		}

		protected virtual IToolCall ParseStreamingToolCall(JObject toolCall, PartialMessageToolCallContext context, IEnumerable<ITool> tools)
		{
			var id = toolCall["id"]?.Value<string>();
			if (id != null)
				context.ToolCallId = id;

			var type = toolCall["type"]?.Value<string>();
			if (type != null)
				context.ToolCallType = type;

			var function = toolCall["function"] as JObject;
			if (function != null)
			{
				var name = function["name"]?.Value<string>();
				if (name != null)
					context.ToolCallFunctionName = name;

				var args = function["arguments"]?.Value<string>();
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

							var parsedArgs = JToken.Parse(context.ToolCallFunctionArguments.ToString());
							return new FunctionToolCall(context.ToolCallId, context.ToolCallFunctionName, parsedArgs);
						}
					}
					catch
					{
					}
				}
			}

			return null;
		}

		protected virtual AssistantMessage ParseNonStreamingAssistantMessage(JObject message, LLModelDescriptor model, IEnumerable<ITool> tools)
		{
			var content = message["content"]?.Value<string>();
			var reasoningContent = message["reasoning_content"]?.Value<string>(); // The DeepSeek is using that currently, why OpenAI can't?

			var parsedToolCalls = !(message["tool_calls"] is JArray toolCalls)
				? new List<IToolCall>()
				: toolCalls.Select(c => ParseToolCall(c as JObject, tools)).ToList();

			return new AssistantMessage(content, reasoningContent, parsedToolCalls);
		}

		protected virtual void ParseStreamingAssistantMessage(JObject delta, PartialAssistantMessage message,
			List<PartialMessageToolCallContext> contexts, IEnumerable<ITool> tools)
		{
			var content = delta["content"]?.Value<string>();
			var reasoningContent = delta["reasoning_content"]?.Value<string>();

			var toolCalls = delta["tool_calls"] as JArray;
			var parsedToolCalls = new List<IToolCall>();

			if (toolCalls != null)
			{
				foreach (var toolCall in toolCalls.Cast<JObject>())
				{
					var index = toolCall["index"]?.Value<int>() ?? 0;
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

		protected virtual void AppendUsage(JObject usage, LLModelDescriptor model)
		{
			var promptTokens = usage["prompt_tokens"]?.Value<int>() ?? -1;
			var completionTokens = usage["completion_tokens"]?.Value<int>() ?? -1;

			TokenUsageStatsCollector.AppendUsage(Name, model.Name, promptTokens, completionTokens);
		}

		protected override async Task<CompletionResult> CreateCompletionsOverrideAsync(LLModelDescriptor model,
			string prompt, string suffix, int count, ICompletionProperties properties, CancellationToken cancellationToken)
		{
			var body = BuildCompletionRequestBody(model, prompt, suffix, properties, count, false);
			var headers = GetRequestHeaders();

			var response = await RequestUtility.GetResponseAsync<JObject>(RequestType.Post, _endpoint.GenerateCompletion,
				body, _http, headers, cancellationToken);

			var error = response["error"] as JObject ?? response;
			var errorCode = error["code"]?.Value<string>();
			if (errorCode != null)
				throw new InvalidDataException($"Error {errorCode}: {error["message"]}"); // TODO: Change to custom exception

			var choices = response["choices"] as JArray;
			if (choices == null || choices.Count == 0)
				throw new InvalidDataException("No choices in response.");

			List<Completion> results = new List<Completion>();

			foreach (JObject choice in choices)
			{
				results.Add(new Completion(choice["text"]?.Value<string>()));
			}

			var usage = response["usage"] as JObject;
			if (usage != null)
				AppendUsage(usage, model);

			return new CompletionResult(this, model, results);
		}

		protected override Task<PartialCompletionResult> CreateStreamingCompletionsOverrideAsync(LLModelDescriptor model,
			string prompt, string suffix, int count, ICompletionProperties properties, CancellationToken cancellationToken)
		{
			var results = Enumerable.Range(0, count).Select(i => new PartialCompletion()).ToImmutableArray();

			try
			{
				var body = BuildCompletionRequestBody(model, prompt, suffix, properties, count, true);
				var headers = GetRequestHeaders();

				Task.Run(() => RequestUtility.ProcessStreamingJsonResponseAsync<JObject>(RequestType.Post, _endpoint.GenerateCompletion,
					body, data =>
					{
						var choices = data["choices"] as JArray;
						if (choices == null || choices.Count == 0)
							throw new InvalidDataException("No choices in response.");

						foreach (JObject choice in choices)
						{
							int index = choice["index"].Value<int>();
							var completion = results[index];

							string? finishReason = choice["finish_reason"]?.Value<string>();
							if (finishReason != null)
							{
								completion.Complete();
							}

							string? delta = choice["text"]?.Value<string>();
							if (delta != null)
								completion.Add(delta);
						}

						var usage = data["usage"] as JObject;
						if (usage != null)
							AppendUsage(usage, model);
					}, _http, headers, cancellationToken))
					.ContinueWith(t =>
					{
						foreach (var completion in results)
						{
							if (completion.CompletionToken.IsCompleted)
								continue;
							if (t.IsFaulted)
								completion.Fail(t.Exception);
							else if (t.IsCanceled)
								completion.Cancel();
						}
					}, TaskScheduler.Default);

			}
			catch (Exception exc)
			{
				Log.Error(exc, "Error in streaming request");
			}

			return Task.FromResult(new PartialCompletionResult(this, model, results));
		}

		private JObject BuildCompletionRequestBody(LLModelDescriptor model, string prompt, string suffix, ICompletionProperties properties, int count, bool stream)
		{
			var result = new JObject
			{
				["model"] = model.Name,
				["prompt"] = prompt,
				["n"] = count,
				["stream"] = stream
			};

			if (!string.IsNullOrEmpty(suffix))
				result["suffix"] = suffix;

			PopulateBodyWithProperties(result, model, OutputFormatDefinition.Empty, Enumerable.Empty<ITool>(), properties);

			return result;
		}
	}
}