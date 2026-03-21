using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Completions.Properties;
using RCLargeLanguageModels.Embeddings;
using RCLargeLanguageModels.Exceptions;
using RCLargeLanguageModels.Formats;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Messages.Attachments;
using RCLargeLanguageModels.Metadata;
using RCLargeLanguageModels.Security;
using RCLargeLanguageModels.Statistics;
using RCLargeLanguageModels.Tools;
using RCLargeLanguageModels.Utilities;
using Serilog;

namespace RCLargeLanguageModels.Clients.Ollama
{
	public class OllamaEndpointConfig : LLMEndpointConfig
	{
		public override string GenerateChatCompletion => BaseUri + "/api/chat";
		public override string GenerateCompletion => BaseUri + "/api/generate";
		public override string GenerateEmbedding => BaseUri + "/api/embed";
		public override string ListModels => BaseUri + "/api/tags";
		public virtual string GetVersion => BaseUri + "/api/version";

		public OllamaEndpointConfig(string baseUri) : base(baseUri)
		{
		}
	}

	/// <summary>
	/// Represents a client for interacting with the Ollama API.
	/// </summary>
	public class OllamaClient : LLMClient
	{
		/// <summary>
		/// The default base URI for the Ollama API.
		/// </summary>
		public const string DefaultBaseUri = "http://127.0.0.1:11434";

		private readonly HttpClient _http;
		private readonly OllamaEndpointConfig _endpoint;
		private readonly ITokenAccessor? _apiKeyAccessor;
		private Version? _version = null;

		/// <summary>
		/// Creates a new instance of the Ollama client using the default base URI.
		/// </summary>
		public OllamaClient()
		{
			_http = CreateHttpClient();
			_endpoint = new OllamaEndpointConfig(DefaultBaseUri);
		}

		/// <summary>
		/// Creates a new instance of the Ollama client using the specified base URI.
		/// </summary>
		/// <param name="baseUri">The base URI of the Ollama server.</param>
		/// <param name="apiKey">The API key to use for authentication for the cloud version of the API.</param>
		/// <param name="http">The HTTP client to use for making requests.</param>
		/// <param name="serverVersion">The version of the server.</param>
		public OllamaClient(string baseUri, string? apiKey = null, HttpClient? http = null, Version? serverVersion = null)
		{
			_http = http ?? CreateHttpClient();
			_apiKeyAccessor = apiKey == null ? null : new StringTokenAccessor(apiKey);
			_endpoint = new OllamaEndpointConfig(baseUri ?? throw new ArgumentNullException(nameof(baseUri)));
			_version = serverVersion;
		}

		/// <summary>
		/// Creates a new instance of the Ollama client using the specified base URI.
		/// </summary>
		/// <param name="baseUri">The base URI of the Ollama server.</param>
		/// <param name="apiKeyAccessor">The API key accessor to use for authentication for the cloud version of the API.</param>
		/// <param name="http">The HTTP client to use for making requests.</param>
		/// <param name="serverVersion">The version of the server.</param>
		public OllamaClient(string baseUri, ITokenAccessor? apiKeyAccessor, HttpClient? http = null, Version? serverVersion = null)
		{
			_http = http ?? CreateHttpClient();
			_apiKeyAccessor = apiKeyAccessor;
			_endpoint = new OllamaEndpointConfig(baseUri ?? throw new ArgumentNullException(nameof(baseUri)));
			_version = serverVersion;
		}

		/// <summary>
		/// Creates a new instance of the Ollama client using the specified endpoint configuration.
		/// </summary>
		/// <param name="endpointConfig">The endpoint configuration for the Ollama client.</param>
		/// <param name="http">The HTTP client to use for making requests.</param>
		/// <param name="serverVersion">The version of the server.</param>
		public OllamaClient(OllamaEndpointConfig endpointConfig, HttpClient? http = null, Version? serverVersion = null)
		{
			_http = http ?? CreateHttpClient();
			_endpoint = endpointConfig ?? throw new ArgumentNullException(nameof(endpointConfig));
			_version = serverVersion;
		}

		public override string Name => "ollama";
		public override string DisplayName => "Ollama";

		public override LLMCapabilities Capabilities =>
			LLMCapabilities.ChatCompletions | LLMCapabilities.SuffixCompletions | LLMCapabilities.Embeddings |
			LLMCapabilities.Reasoning | LLMCapabilities.ToolSupport | LLMCapabilities.Vision | LLMCapabilities.StreamingCompletions;

		public override OutputFormatSupportSet SupportedOutputFormats =>
			OutputFormatSupportSet.TextWithJsonSchema;

		protected virtual Dictionary<string, string> GetRequestHeaders()
		{
			var result = new Dictionary<string, string>();

			if (_apiKeyAccessor != null)
				result["Authorization"] = $"Bearer {_apiKeyAccessor.GetToken()}";

			return result;
		}

		protected override async Task<LLModelDescriptor[]> ListModelsOverrideAsync(CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(_endpoint.ListModels))
				return Array.Empty<LLModelDescriptor>();

			var headers = GetRequestHeaders();
			var response = await RequestUtility.GetResponseAsync<JsonObject>(
				RequestType.Get, _endpoint.ListModels, body: null, client: _http, headers: headers, cancellationToken: CancellationToken.None);

			var result = new List<LLModelDescriptor>();

			var models = response["models"]!.AsArray();
			foreach (var model in models)
			{
				var name = model?["name"]?.GetValue<string>();
				if (name != null)
				{
					try
					{
						result.Add(OllamaModels.GetModelDescriptor(this, name));
					}
					catch { } // Swallow exceptions
				}
			}

			return result.ToArray();
		}

		/// <summary>
		/// Gets the version of the Ollama server.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token used to cancel the request.</param>
		/// <returns>The version of the Ollama server.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the EndpointConfig is not of type <see cref="OllamaEndpointConfig"/>.</exception>
		public async Task<Version> GetVersionAsync(CancellationToken cancellationToken = default)
		{
			if (_version != null)
				return _version;

			var headers = GetRequestHeaders();
			var response = await RequestUtility.GetResponseAsync<JsonObject>(
				RequestType.Get, _endpoint.GetVersion, body: null, client: _http, headers: headers, cancellationToken: CancellationToken.None);

			var versionStr = (response["version"]?.GetValue<string>()) ??
				throw new InvalidOperationException("Could not get version from response");
			_version = Version.Parse(versionStr);
			return _version;
		}

		protected override async Task<ChatCompletionResult> CreateChatCompletionsOverrideAsync(
			LLModelDescriptor model,
			List<IMessage> messages,
			int count,
			List<CompletionProperty> properties,
			OutputFormatDefinition outputFormatDefinition,
			ToolSet tools,
			CancellationToken cancellationToken)
		{
			await GetVersionAsync(cancellationToken);

			var headers = GetRequestHeaders();
			var body = BuildChatRequestBody(model, messages, false, properties, outputFormatDefinition, tools);

			var response = await RequestUtility.GetResponseAsync<JsonObject>(
				RequestType.Post,
				_endpoint.GenerateChatCompletion,
				body, _http, headers, cancellationToken: cancellationToken);

			bool gettingThinking = false;
			var delta = ParseChatResponse(response, model, tools, false, ref gettingThinking).Delta;

			return new ChatCompletionResult(this, model, new AssistantMessage(delta.DeltaContent, delta.DeltaReasoningContent, delta.NewToolCalls));
		}

		protected override async Task<PartialChatCompletionResult> CreateStreamingChatCompletionsOverrideAsync(
			LLModelDescriptor model,
			List<IMessage> messages,
			int count,
			List<CompletionProperty> properties,
			OutputFormatDefinition outputFormatDefinition,
			ToolSet tools,
			CancellationToken cancellationToken)
		{
			await GetVersionAsync(cancellationToken);

			var headers = GetRequestHeaders();
			var body = BuildChatRequestBody(model, messages, true, properties, outputFormatDefinition, tools);

			bool gettingThinking = false;
			var message = new PartialAssistantMessage();

			void OnDataReceived(JsonObject response)
			{
				var (delta, metadata) = ParseChatResponse(response, model, tools, false, ref gettingThinking);
				message.Add(delta);
				if (metadata != null)
					message.Complete(metadata);
			}

			var _ = RequestUtility.ProcessStreamingJsonResponseAsync<JsonObject>(
				RequestType.Post,
				_endpoint.GenerateChatCompletion,
				body, OnDataReceived, _http, headers, cancellationToken: cancellationToken)
				.ContinueWith(t =>
				{
					if (t.IsFaulted)
						message.Fail(t.Exception);
					else if (t.IsCanceled)
						message.Cancel();
				}, cancellationToken);

			return new PartialChatCompletionResult(this, model, message);
		}

		protected override async Task<CompletionResult> CreateCompletionsOverrideAsync(
			LLModelDescriptor model,
			string prompt,
			string? suffix,
			int count,
			List<CompletionProperty> properties,
			CancellationToken cancellationToken)
		{
			await GetVersionAsync(cancellationToken);

			var headers = GetRequestHeaders();
			var body = BuildRequestBody(model, prompt, suffix, false, properties);

			var response = await RequestUtility.GetResponseAsync<JsonObject>(
				RequestType.Post,
				_endpoint.GenerateCompletion,
				body, _http, headers, cancellationToken: cancellationToken);

			var (delta, metadata) = ParseCompletionResponse(response, model, false);

			return new CompletionResult(this, model, new Completion(delta.DeltaContent), metadata);
		}

		protected override async Task<PartialCompletionResult> CreateStreamingCompletionsOverrideAsync(
			LLModelDescriptor model,
			string prompt,
			string? suffix,
			int count,
			List<CompletionProperty> properties,
			CancellationToken cancellationToken)
		{
			await GetVersionAsync(cancellationToken);

			var headers = GetRequestHeaders();
			var body = BuildRequestBody(model, prompt, suffix, true, properties);

			var completion = new PartialCompletion();

			void OnStreamResponse(JsonObject response)
			{
				var (delta, metadata) = ParseCompletionResponse(response, model, false);
				completion.Add(delta);

				if (metadata != null)
					completion.Complete(metadata);
			}

			var _ = RequestUtility.ProcessStreamingJsonResponseAsync<JsonObject>(
				RequestType.Post,
				_endpoint.GenerateChatCompletion,
				body, OnStreamResponse, _http, headers, cancellationToken: cancellationToken)
				.ContinueWith(t =>
				{
					if (t.IsFaulted)
						completion.Fail(t.Exception);
					else if (t.IsCanceled)
						completion.Cancel();
				}, cancellationToken);

			return new PartialCompletionResult(this, model, completion);
		}

		protected override async Task<EmbeddingResult> CreateEmbeddingsOverrideAsync(
			LLModelDescriptor model,
			List<string> inputs,
			List<CompletionProperty> properties,
			CancellationToken cancellationToken)
		{
			await GetVersionAsync(cancellationToken);

			var body = BuildEmbeddingRequestBody(model, inputs, properties);

			var response = await RequestUtility.GetResponseAsync(
				RequestType.Post,
				_endpoint.GenerateEmbedding,
				body, _http, cancellationToken: cancellationToken);
			response.EnsureSuccessStatusCode();
			var responseContent = await response.ParseContentAsync<JsonObject>(cancellationToken);

			var embeddings = ParseEmbeddingsResponse(responseContent, model);

			var metadata = new List<IMetadata>();
			metadata.Add(GetUsageMetadata(responseContent));

			return new EmbeddingResult(this, model, embeddings, metadata);
		}

		private JsonObject BuildRequestBody(LLModelDescriptor model, string prompt, string? suffix,
			bool stream, IEnumerable<CompletionProperty> _properties)
		{
			var properties = _properties as List<CompletionProperty> ?? _properties.ToList();

			var result = new JsonObject
			{
				["model"] = model.Name,
				["prompt"] = prompt,
				["stream"] = stream,
				["options"] = BuildOptions(properties)
			};

			if (!string.IsNullOrEmpty(suffix))
				result["suffix"] = suffix;

			PopulateBodyWithProperties(result, model, properties, OutputFormatDefinition.Empty);

			return result;
		}

		private (CompletionDelta Delta, IEnumerable<IMetadata>? Metadata) ParseCompletionResponse(JsonObject response, LLModelDescriptor model, bool stream)
		{
			var content = response["response"]?.GetValue<string>();

			List<IMetadata>? metadata = null;
			var done = response["done"]?.GetValue<bool>() ?? false;
			if (done)
			{
				metadata = new List<IMetadata>();
				metadata.Add(GetFinishReasonMetadata(response["done_reason"]?.GetValue<string>() ?? ""));
				metadata.Add(GetUsageMetadata(response));
			}

			return (new CompletionDelta(content), metadata);
		}

		private const string ThinkingTagOpen = "<think>";
		private const string ThinkingTagClose = "</think>";

		protected virtual (string content, string reasoningContent) ParseMessageContent(
			JsonObject message, LLModelDescriptor model, bool stream, ref bool gettingThinking)
		{
			var content = message["content"]?.GetValue<string>() ?? string.Empty;
			var reasoningContent = string.Empty;

			if (model.Capabilities.IsReasoning())
			{
				if (_version >= new Version(0, 9, 0))
				{
					// Ollama 0.9.x and later sends the explicit reasoning content in the message

					reasoningContent = message["thinking"]?.GetValue<string>() ?? string.Empty;
				}
				else if (stream)
				{
					// Ollama 0.8.x and earlier sends the exact thinking tags in the streaming response chunks:
					// '<think>' 'Hello' '!' '</think>'
					// So we can just compare them

					if (content == ThinkingTagOpen && !gettingThinking)
					{
						gettingThinking = true;
						content = string.Empty;
					}
					else if (content == ThinkingTagClose && gettingThinking)
					{
						gettingThinking = false;
						content = string.Empty;
					}
					else if (gettingThinking)
					{
						content = string.Empty;
						reasoningContent = content;
					}
				}
				else
				{
					// We consider that's there is one or zero thinking blocks in the message
					// TODO: Change if the reasoning content is not always the first block
					// (when reasoning models will have multiple reasoning blocks)

					if (content.StartsWith(ThinkingTagOpen))
					{
						// The message contains reasoning content

						int index = content.IndexOf(ThinkingTagClose, ThinkingTagOpen.Length);
						if (index != -1)
						{
							reasoningContent = content.Substring(ThinkingTagOpen.Length, index).Trim();
							content = content.Substring(index + ThinkingTagClose.Length);
						}
					}
				}
			}

			return (content, reasoningContent);
		}

		protected virtual IEnumerable<IToolCall> ParseToolCalls(
			JsonArray toolCalls, LLModelDescriptor model, IEnumerable<ITool> tools)
		{
			List<IToolCall> result = new List<IToolCall>(toolCalls.Count);
			foreach (var toolCall in toolCalls)
			{
				var function = toolCall!["function"]!.AsObject();
				if (function != null)
				{
					var name = function["name"]?.GetValue<string>() ?? string.Empty;
					var tool = tools.FirstOrDefault(t => t.Name == name);
					if (tool is FunctionTool functionTool)
					{
						var call = new FunctionToolCall(ToolCallId.Generate(0), name, function["arguments"]!);
						result.Add(call);
					}
				}
			}
			return result;
		}

		protected virtual (AssistantMessageDelta Delta, IEnumerable<IMetadata>? CompletionMetadata) ParseChatResponse(
			JsonObject response, LLModelDescriptor model, IEnumerable<ITool> tools, bool stream, ref bool gettingThinking)
		{
			var message = response["message"] as JsonObject;
			if (message == null)
				throw new LLMException("Could not get message from response.", model);

			List<IMetadata>? metadata = null;
			if (response["done"]?.GetValue<bool>() ?? false)
			{
				metadata = new List<IMetadata>();
				metadata.Add(GetFinishReasonMetadata(response["done_reason"]?.GetValue<string>() ?? ""));
				metadata.Add(GetUsageMetadata(response));
			}

			var (content, reasoningContent) = ParseMessageContent(message, model, stream, ref gettingThinking);

			var toolCalls = message["tool_calls"] as JsonArray;
			IEnumerable<IToolCall> toolCallsParsed = Enumerable.Empty<IToolCall>();
			if (toolCalls != null)
				toolCallsParsed = ParseToolCalls(toolCalls, model, tools);

			return (new AssistantMessageDelta(content, reasoningContent, toolCallsParsed), metadata);
		}

		protected virtual JsonObject BuildChatRequestBody(
			LLModelDescriptor model,
			IEnumerable<IMessage> _messages,
			bool stream,
			IEnumerable<CompletionProperty> _properties,
			OutputFormatDefinition outputFormatDefinition,
			IEnumerable<ITool> tools)
		{
			var messages = _messages as List<IMessage> ?? _messages.ToList();
			var builtMessages = new List<JsonObject>(messages.Count);
			int c = 0, lastIndex = messages.Count - 1;
			var properties = _properties as List<CompletionProperty> ?? _properties.ToList();

			foreach (var message in messages)
			{
				builtMessages.Add(BuildMessage(message, c == lastIndex));
				c++;
			}

			var result = new JsonObject
			{
				["model"] = model.Name,
				["messages"] = new JsonArray(builtMessages.ToArray()),
				["stream"] = stream,
				["options"] = BuildOptions(properties)
			};

			PopulateBodyWithProperties(result, model, properties, outputFormatDefinition);

			if (tools.Any())
			{
				result["tools"] = new JsonArray(tools.Select(BuildTool).ToArray());
			}

			// The 0.9.0 version of Ollama introduced explicit thinking support (no need to parse <think> tags)
			if (_version >= new Version(0, 9, 0))
			{
				if (model.Capabilities.IsReasoning() || model.Capabilities.IsUnknown())
					result["think"] = properties.OfType<ThinkProperty>().FirstOrDefault()?.Value ?? true;
			}

			return result;
		}

		protected virtual JsonObject BuildOptions(List<CompletionProperty> properties)
		{
			var body = new JsonObject();

			foreach (var property in properties)
			{
				var propertyName = property.Name;
				var value = property.RawValue;

				switch (property)
				{
					case MaxTokensProperty:
						propertyName = "num_predict";
						break;
					case StopSequencesProperty ssp:
						value = ssp.Value.ToArray();
						break;

					case KeepAliveProperty:
					case ThinkProperty:
						continue;
				}

				body.Add(propertyName, JsonSerializer.SerializeToNode(value));
			}

			return body;
		}

		protected virtual void PopulateBodyWithProperties(JsonObject body,
			LLModelDescriptor model,
			List<CompletionProperty> properties,
			OutputFormatDefinition outputFormatDefinition)
		{
			if (outputFormatDefinition.Type == OutputFormatType.Json)
				body["format"] = "json";
			else if (outputFormatDefinition is JsonSchemaOutputFormatDefinition jsonSchemaOutput)
				body["format"] = jsonSchemaOutput.Schema;
			
			var keepAlive = properties.OfType<KeepAliveProperty>().FirstOrDefault()?.Value;

			if (keepAlive.HasValue)
			{
				var timeSpan = keepAlive.Value;

				if (timeSpan.TotalSeconds < 1)
				{
					body["keep_alive"] = "0s";
				}
				else
				{
					var parts = new List<string>();

					if (timeSpan.Hours > 0)
						parts.Add($"{Math.Floor(timeSpan.TotalHours)}h");
					if (timeSpan.Minutes > 0)
						parts.Add($"{timeSpan.Minutes}m");
					if (timeSpan.Seconds > 0)
						parts.Add($"{timeSpan.Seconds}s");

					body["keep_alive"] = string.Join(" ", parts);
				}
			}
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
							["parameters"] = functionTool.ArgumentSchema.DeepClone()
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
					var ures = new JsonObject
					{
						["role"] = "user",
						["content"] = userMessage.Content
					};

					var imageAttachments = userMessage.Attachments.OfType<IImageAttachment>().ToList();
					if (imageAttachments.Count > 0)
					{
						ures["images"] = new JsonArray(imageAttachments.Select(a => (JsonNode)a.GetBase64()).ToArray());
					}

					return ures;

				case IAssistantMessage assistantMessage:
					var ares = new JsonObject
					{
						["role"] = "assistant",
						["content"] = assistantMessage.Content
					};

					var toolCalls = new JsonArray(assistantMessage.ToolCalls.Select(BuildToolCall).ToArray());
					if (toolCalls.Count > 0)
						ares["tool_calls"] = toolCalls;

					return ares;

				case IToolMessage toolMessage:
					var tres = new JsonObject
					{
						["role"] = "tool",
						["tool_call_id"] = toolMessage.ToolCallId,
						["content"] = toolMessage.Content
					};

					imageAttachments = toolMessage.Attachments.OfType<IImageAttachment>().ToList();
					if (imageAttachments.Count > 0)
					{
						tres["images"] = new JsonArray(imageAttachments.Select(a => (JsonNode)a.GetBase64()).ToArray());
					}

					return tres;

				default:
					throw new InvalidOperationException("Unknown message type.");
			}
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

		protected virtual IUsageMetadata GetUsageMetadata(JsonObject response)
		{
			var promptEvalCount = response["prompt_eval_count"]?.GetValue<int>() ?? 0;
			var evalCount = response["eval_count"]?.GetValue<int>() ?? 0;

			return new UsageMetadata(promptEvalCount, evalCount);
		}

		protected virtual JsonObject BuildEmbeddingRequestBody(
			LLModelDescriptor model,
			IEnumerable<string> inputs,
			IEnumerable<CompletionProperty> properties)
		{
			var inputsList = inputs.ToList();
			var propertiesList = properties.ToList();

			var result = new JsonObject
			{
				["model"] = model.Name,
				["options"] = BuildOptions(propertiesList)
			};

			if (inputsList.Count == 1)
			{
				result["input"] = inputsList[0];
			}
			else
			{
				result["input"] = new JsonArray(inputsList.Select(i => JsonValue.Create(i)).ToArray());
			}

			var keepAlive = propertiesList.OfType<KeepAliveProperty>().FirstOrDefault()?.Value;
			if (keepAlive.HasValue)
			{
				result["keep_alive"] = FormatKeepAlive(keepAlive.Value);
			}

			var truncate = propertiesList.OfType<TruncateProperty>().FirstOrDefault()?.Value;
			if (truncate.HasValue)
			{
				result["truncate"] = truncate.Value;
			}

			return result;
		}

		protected virtual List<Embedding> ParseEmbeddingsResponse(JsonObject response, LLModelDescriptor model)
		{
			var embeddings = new List<Embedding>();

			if (response["embeddings"] is JsonArray embeddingsArray)
			{
				foreach (var embeddingNode in embeddingsArray)
				{
					if (embeddingNode is JsonArray vectorArray)
					{
						var vector = ParseEmbeddingVector(vectorArray);
						embeddings.Add(new Embedding(vector, model));
					}
				}
			}
			else if (response["embedding"] is JsonArray singleEmbeddingArray)
			{
				var vector = ParseEmbeddingVector(singleEmbeddingArray);
				embeddings.Add(new Embedding(vector, model));
			}
			else
			{
				throw new LLMException("No embeddings found in response.", model);
			}

			return embeddings;
		}

		protected virtual float[] ParseEmbeddingVector(JsonArray vectorArray)
		{
			var vector = new float[vectorArray.Count];
			for (int i = 0; i < vectorArray.Count; i++)
			{
				vector[i] = vectorArray[i]!.GetValue<float>();
			}
			return vector;
		}

		protected virtual string FormatKeepAlive(TimeSpan timeSpan)
		{
			if (timeSpan.TotalSeconds < 1)
			{
				return "0s";
			}

			var parts = new List<string>();

			if (timeSpan.Hours > 0)
				parts.Add($"{Math.Floor(timeSpan.TotalHours)}h");
			if (timeSpan.Minutes > 0)
				parts.Add($"{timeSpan.Minutes}m");
			if (timeSpan.Seconds > 0)
				parts.Add($"{timeSpan.Seconds}s");

			return string.Join(" ", parts);
		}
	}
}