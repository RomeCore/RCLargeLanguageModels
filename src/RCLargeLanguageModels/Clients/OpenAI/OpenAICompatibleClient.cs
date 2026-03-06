using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Formats;
using RCLargeLanguageModels.Security;
using RCLargeLanguageModels.Statistics;
using RCLargeLanguageModels.Tools;
using RCLargeLanguageModels.Utilities;
using Serilog;
using RCLargeLanguageModels.Completions;
using System.Net.Http;
using System.Collections.Immutable;
using RCLargeLanguageModels.Completions.Properties;
using System.Text.Json.Nodes;
using System.Text.Json;
using RCLargeLanguageModels.Embeddings;

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
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public OpenAICompatibleClient(string baseUri, string apiKey, HttpClient? http = null)
		{
			_apiKeyAccessor = new StringTokenAccessor(apiKey);
			_http = http ?? CreateHttpClient();
			_endpoint = new OpenAIEndpointConfig(baseUri ?? throw new ArgumentNullException(nameof(baseUri)));
		}

		/// <summary>
		/// Creates a new instance of the OpenAI-compatible client using the specified base URI and API key accessor.
		/// </summary>
		/// <param name="baseUri">The base URI of the OpenAI-compatible API.</param>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public OpenAICompatibleClient(string baseUri, ITokenAccessor tokenAccessor, HttpClient? http = null)
		{
			_apiKeyAccessor = tokenAccessor ?? throw new ArgumentNullException(nameof(tokenAccessor));
			_http = http ?? CreateHttpClient();
			_endpoint = new OpenAIEndpointConfig(baseUri ?? throw new ArgumentNullException(nameof(baseUri)));
		}

		/// <summary>
		/// Creates a new instance of the OpenAI-compatible client using the specified endpoint configuration and API key.
		/// </summary>
		/// <param name="endpointConfig">The endpoint configuration for the OpenAI-compatible API.</param>
		/// <param name="apiKey">The API key for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public OpenAICompatibleClient(LLMEndpointConfig endpointConfig, string apiKey, HttpClient? http = null)
		{
			_apiKeyAccessor = new StringTokenAccessor(apiKey);
			_http = http ?? CreateHttpClient();
			_endpoint = endpointConfig ?? throw new ArgumentNullException(nameof(endpointConfig));
		}

		/// <summary>
		/// Creates a new instance of the OpenAI-compatible client using the specified endpoint configuration and API key accessor.
		/// </summary>
		/// <param name="endpointConfig">The endpoint configuration for the OpenAI-compatible API.</param>
		/// <param name="tokenAccessor">The API key accessor for authentication.</param>
		/// <param name="http">The HTTP client to use for requests. If not provided, a default one will be created.</param>
		public OpenAICompatibleClient(LLMEndpointConfig endpointConfig, ITokenAccessor tokenAccessor, HttpClient? http = null)
		{
			_apiKeyAccessor = tokenAccessor ?? throw new ArgumentNullException(nameof(tokenAccessor));
			_http = http ?? CreateHttpClient();
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

			try
			{
				var response = await RequestUtility.GetResponseAsync(RequestType.Get, _endpoint.ListModels,
					null, _http, headers, cancellationToken);

				var responseContent = await response.ParseContentAsync<JsonObject>(cancellationToken);

				var models = responseContent["data"] as JsonArray;
				if (models == null)
					throw new LLMException("No models in response.");

				var result = new List<LLModelDescriptor>();
				foreach (var model in models)
				{
					var id = model["id"]?.GetValue<string>();
					result.Add(new LLModelDescriptor(this, id));
				}

				return result.ToArray();
			}
			catch (Exception ex)
			{
				throw new LLMException("Failed to list models.", ex);
			}
		}

		protected override async Task<ChatCompletionResult> CreateChatCompletionsOverrideAsync(LLModelDescriptor model,
			IEnumerable<IMessage> messages, int count, IEnumerable<CompletionProperty> properties, OutputFormatDefinition outputFormatDefinition, IEnumerable<ITool> tools, CancellationToken cancellationToken)
		{
			var body = BuildChatRequestBody(model, messages, outputFormatDefinition, tools, properties, count, false);
			var headers = GetRequestHeaders();

			var response = await RequestUtility.GetResponseAsync(RequestType.Post, _endpoint.GenerateChatCompletion,
				body, _http, headers, cancellationToken);

			var responseContent = await response.ParseContentAsync<JsonObject>(cancellationToken);

			var error = responseContent["error"] as JsonObject ?? responseContent;
			var errorCode = error["code"]?.GetValue<string>();
			if (errorCode != null)
				throw new LLMException($"Error {errorCode}: {error["message"]}");

			var choices = responseContent["choices"] as JsonArray;
			if (choices == null || choices.Count == 0)
				throw new InvalidDataException("No choices in response.");

			List<AssistantMessage> resultMessages = new List<AssistantMessage>();

			foreach (JsonObject choice in choices)
			{
				var message = choice["message"] as JsonObject
					?? throw new InvalidDataException("No message in 'choice'.");

				resultMessages.Add(ParseNonStreamingAssistantMessage(message, model, tools));
			}

			var usage = responseContent["usage"] as JsonObject;
			if (usage != null)
				AppendUsage(usage, model);

			return new ChatCompletionResult(this, model, resultMessages);
		}

		protected override Task<PartialChatCompletionResult> CreateStreamingChatCompletionsOverrideAsync(LLModelDescriptor model, IEnumerable<IMessage> messages, int count, IEnumerable<CompletionProperty> properties, OutputFormatDefinition outputFormatDefinition, IEnumerable<ITool> tools, CancellationToken cancellationToken)
		{
			var resultMessages = Enumerable.Range(0, count).Select(i => new PartialAssistantMessage()).ToImmutableArray();
			var contexts = Enumerable.Range(0, count).Select(i => new List<PartialMessageToolCallContext>()).ToImmutableArray();

			try
			{
				var body = BuildChatRequestBody(model, messages, outputFormatDefinition, tools, properties, count, true);
				var headers = GetRequestHeaders();

				string bodyStr = body.ToString();

				Task.Run(() => RequestUtility.ProcessStreamingJsonResponseAsync<JsonObject>(RequestType.Post, _endpoint.GenerateChatCompletion,
					body, data =>
					{
						var choices = data["choices"] as JsonArray;
						if (choices == null || choices.Count == 0)
							throw new InvalidDataException("No choices in response.");

						foreach (JsonObject choice in choices)
						{
							int index = choice["index"].GetValue<int>();
							var message = resultMessages[index];

							string? finishReason = choice["finish_reason"]?.GetValue<string>();
							if (finishReason != null)
							{
								message.Complete();
							}

							var delta = choice["delta"] as JsonObject
								?? throw new InvalidDataException("No delta in 'choice'.");
							var context = contexts[index];

							ParseStreamingAssistantMessage(delta, message, context, tools);
						}

						var usage = data["usage"] as JsonObject;
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

		protected virtual JsonObject BuildTool(ITool tool)
		{
			switch (tool)
			{
				case FunctionTool functionTool:
					return new JsonObject
					{
						["type"] = "function",
						["function"] = new JsonObject
						{
							["name"] = functionTool.Name,
							["description"] = functionTool.Description,
							["parameters"] = functionTool.ArgumentSchema
						}
					};

				default:
					throw new InvalidOperationException("Unknown tool type");
			}
		}

		protected virtual JsonObject BuildToolCall(IToolCall toolCall)
		{
			switch (toolCall)
			{
				case FunctionToolCall functionCall:

					return new JsonObject
					{
						["id"] = functionCall.Id,
						["type"] = "function",
						["function"] = new JsonObject
						{
							["name"] = functionCall.ToolName,
							["arguments"] = functionCall.Args
						}
					};

				default:
					throw new InvalidOperationException("Unknown tool call type");
			}
		}

		protected virtual JsonObject BuildMessage(IMessage message, bool isLast)
		{
			switch (message)
			{
				case ISystemMessage systemMessage:
					return new JsonObject
					{
						["role"] = "system",
						["content"] = systemMessage.Content
					};

				case IUserMessage userMessage:
					return new JsonObject
					{
						["role"] = "user",
						["content"] = userMessage.BuildContentWithTextAttachments()
					};

				case IAssistantMessage assistantMessage:
					var res = new JsonObject
					{
						["role"] = "assistant",
						["content"] = assistantMessage.BuildContentWithTextAttachments()
					};

					var toolCalls = new JsonArray(assistantMessage.ToolCalls.Select(BuildToolCall).ToArray());
					if (toolCalls.Count > 0)
						res["tool_calls"] = toolCalls;

					return res;

				case IToolMessage toolMessage:
					return new JsonObject
					{
						["role"] = "tool",
						["tool_call_id"] = toolMessage.ToolCallId,
						["content"] = toolMessage.BuildContentWithTextAttachments()
					};

				default:
					throw new InvalidOperationException("Unknown message type.");
			}
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
			var messages = _messages.ToList();
			var builtMessages = new List<JsonObject>(messages.Count);
			int c = 0, lastIndex = messages.Count - 1;

			foreach (var message in messages)
			{
				builtMessages.Add(BuildMessage(message, c == lastIndex));
				c++;
			}

			var result = new JsonObject
			{
				["model"] = model.Name,
				["messages"] = new JsonArray(builtMessages.ToArray()),
				["n"] = count,
				["stream"] = stream
			};

			if (tools.Any())
			{
				result["tools"] = new JsonArray(tools.Select(BuildTool).ToArray());
			}

			PopulateBodyWithProperties(result, model, outputFormatDefinition, tools, properties);

			return result;
		}

		protected virtual IToolCall ParseToolCall(JsonObject toolCall, IEnumerable<ITool> tools)
		{
			var id = toolCall["id"]?.GetValue<string>();
			var type = toolCall["type"]?.GetValue<string>();

			switch (type)
			{
				case "function":

					var function = toolCall["function"] as JsonObject;

					var name = function["name"]?.GetValue<string>()
						?? throw new ArgumentException("Missing 'name' in function tool call");
					var args = function["arguments"]?.GetValue<string>()
						?? throw new ArgumentException("Missing 'arguments' in function tool call");

					var tool = tools.First(t => t.Name == name);
					var functionTool = tool as FunctionTool
						?? throw new ArgumentException($"Tool '{name}' not found in tool set");
					var parsedArgs = JsonNode.Parse(args);

					return new FunctionToolCall(id, name, parsedArgs);

				default:

					throw new InvalidOperationException("Unknown tool call type");
			}
		}

		protected virtual IToolCall ParseStreamingToolCall(JsonObject toolCall, PartialMessageToolCallContext context, IEnumerable<ITool> tools)
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

		protected virtual AssistantMessage ParseNonStreamingAssistantMessage(JsonObject message, LLModelDescriptor model, IEnumerable<ITool> tools)
		{
			var content = message["content"]?.GetValue<string>();
			var reasoningContent = message["reasoning_content"]?.GetValue<string>(); // The DeepSeek is using that currently, why OpenAI can't?

			var parsedToolCalls = !(message["tool_calls"] is JsonArray toolCalls)
				? new List<IToolCall>()
				: toolCalls.Select(c => ParseToolCall(c as JsonObject, tools)).ToList();

			return new AssistantMessage(content, reasoningContent, parsedToolCalls);
		}

		protected virtual void ParseStreamingAssistantMessage(JsonObject delta, PartialAssistantMessage message,
			List<PartialMessageToolCallContext> contexts, IEnumerable<ITool> tools)
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

		protected virtual void AppendUsage(JsonObject usage, LLModelDescriptor model)
		{
			var promptTokens = usage["prompt_tokens"]?.GetValue<int>() ?? -1;
			var completionTokens = usage["completion_tokens"]?.GetValue<int>() ?? -1;

			TokenUsageStatsCollector.AppendUsage(Name, model.Name, promptTokens, completionTokens);
		}

		protected override async Task<CompletionResult> CreateCompletionsOverrideAsync(LLModelDescriptor model,
			string prompt, string suffix, int count, IEnumerable<CompletionProperty> properties, CancellationToken cancellationToken)
		{
			var body = BuildCompletionRequestBody(model, prompt, suffix, properties, count, false);
			var headers = GetRequestHeaders();

			var response = await RequestUtility.GetResponseAsync<JsonObject>(RequestType.Post, _endpoint.GenerateCompletion,
				body, _http, headers, cancellationToken);

			var error = response["error"] as JsonObject ?? response;
			var errorCode = error["code"]?.GetValue<string>();
			if (errorCode != null)
				throw new InvalidDataException($"Error {errorCode}: {error["message"]}"); // TODO: Change to custom exception

			var choices = response["choices"] as JsonArray;
			if (choices == null || choices.Count == 0)
				throw new InvalidDataException("No choices in response.");

			List<Completion> results = new List<Completion>();

			foreach (JsonObject choice in choices)
			{
				results.Add(new Completion(choice["text"]?.GetValue<string>()));
			}

			var usage = response["usage"] as JsonObject;
			if (usage != null)
				AppendUsage(usage, model);

			return new CompletionResult(this, model, results);
		}

		protected override Task<PartialCompletionResult> CreateStreamingCompletionsOverrideAsync(LLModelDescriptor model,
			string prompt, string suffix, int count, IEnumerable<CompletionProperty> properties, CancellationToken cancellationToken)
		{
			var results = Enumerable.Range(0, count).Select(i => new PartialCompletion()).ToImmutableArray();

			try
			{
				var body = BuildCompletionRequestBody(model, prompt, suffix, properties, count, true);
				var headers = GetRequestHeaders();

				Task.Run(() => RequestUtility.ProcessStreamingJsonResponseAsync<JsonObject>(RequestType.Post, _endpoint.GenerateCompletion,
					body, data =>
					{
						var choices = data["choices"] as JsonArray;
						if (choices == null || choices.Count == 0)
							throw new InvalidDataException("No choices in response.");

						foreach (JsonObject choice in choices)
						{
							int index = choice["index"].GetValue<int>();
							var completion = results[index];

							string? finishReason = choice["finish_reason"]?.GetValue<string>();
							if (finishReason != null)
							{
								completion.Complete();
							}

							string? delta = choice["text"]?.GetValue<string>();
							if (delta != null)
								completion.Add(delta);
						}

						var usage = data["usage"] as JsonObject;
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

		private JsonObject BuildCompletionRequestBody(LLModelDescriptor model, string prompt, string suffix, IEnumerable<CompletionProperty> properties, int count, bool stream)
		{
			var result = new JsonObject
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

		protected override async Task<EmbeddingResult> CreateEmbeddingsOverrideAsync(LLModelDescriptor model, IEnumerable<string> inputs, IEnumerable<CompletionProperty>? properties, CancellationToken cancellationToken)
		{
			var body = BuildEmbeddingRequestBody(model, inputs, properties);
			var headers = GetRequestHeaders();

			try
			{
				var response = await RequestUtility.GetResponseAsync(
					RequestType.Post,
					_endpoint.GenerateEmbedding,
					body,
					_http,
					headers,
					cancellationToken);

				var responseContent = await response.ParseContentAsync<JsonObject>(cancellationToken);

				var error = responseContent["error"] as JsonObject ?? responseContent;
				var errorCode = error["code"]?.GetValue<string>();
				if (errorCode != null)
					throw new LLMException($"Error {errorCode}: {error["message"]}");

				var data = responseContent["data"] as JsonArray;
				if (data == null || data.Count == 0)
					throw new InvalidDataException("No embedding data in response.");

				List<Embedding> embeddings = new List<Embedding>();

				foreach (JsonObject item in data)
				{
					int index = item["index"]?.GetValue<int>() ?? -1;

					var embeddingObj = item["embedding"] as JsonArray;
					if (embeddingObj == null)
						throw new InvalidDataException("Missing or invalid embedding vector in response.");

					var vector = new List<float>();
					foreach (var value in embeddingObj)
					{
						vector.Add(value.GetValue<float>());
					}

					embeddings.Add(new Embedding(vector, model));
				}

				var usage = responseContent["usage"] as JsonObject;
				if (usage != null)
					AppendEmbeddingUsage(usage, model);

				return new EmbeddingResult(this, model, embeddings);
			}
			catch (Exception ex)
			{
				throw new LLMException("Failed to create embeddings.", ex);
			}
		}

		protected virtual JsonObject BuildEmbeddingRequestBody(
			LLModelDescriptor model,
			IEnumerable<string> inputs,
			IEnumerable<CompletionProperty>? properties)
		{
			var inputsArray = new JsonArray(inputs.Select(i => JsonValue.Create(i)).ToArray());

			var result = new JsonObject
			{
				["model"] = model.Name,
				["input"] = inputsArray,
				["encoding_format"] = "float"
			};

			if (properties != null)
			{
				foreach (var property in properties)
				{
					switch (property.Name)
					{
						case "encoding_format":
							result["encoding_format"] = JsonValue.Create(property.RawValue?.ToString() ?? "float");
							break;

						case "dimensions":
							if (property.RawValue is int dims)
								result["dimensions"] = dims;
							break;

						case "user":
							result["user"] = JsonValue.Create(property.RawValue?.ToString());
							break;

						default:
							if (property.RawValue != null)
								result[property.Name] = JsonSerializer.SerializeToNode(property.RawValue);
							break;
					}
				}
			}

			return result;
		}

		protected virtual void AppendEmbeddingUsage(JsonObject usage, LLModelDescriptor model)
		{
			var promptTokens = usage["prompt_tokens"]?.GetValue<int>() ?? -1;
			var totalTokens = usage["total_tokens"]?.GetValue<int>() ?? -1;

			TokenUsageStatsCollector.AppendUsage(Name, model.Name, promptTokens, 0);
		}
	}
}