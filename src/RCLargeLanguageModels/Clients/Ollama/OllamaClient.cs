using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RCLargeLanguageModels.Json;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Formats;
using RCLargeLanguageModels.Statistics;
using RCLargeLanguageModels.Tasks;
using RCLargeLanguageModels.Tools;
using RCLargeLanguageModels.Utilities;
using Serilog;
using System.Net.Http;
using RCLargeLanguageModels.Completions;
using System.IO;
using RCLargeLanguageModels.Completions.Properties;

namespace RCLargeLanguageModels.Clients.Ollama
{
	public class OllamaEndpointConfig : LLMEndpointConfig
	{
		public override string GenerateChatCompletion => BaseUri + "/api/chat";
		public override string GenerateCompletion => BaseUri + "/api/generate";
		public override string ListModels => BaseUri + "/api/tags";
		public virtual string GetVersion => BaseUri + "/api/version";

		public OllamaEndpointConfig(string baseUri) : base(baseUri)
		{
		}
	}

	/// <summary>
	/// Represents a client for interacting with the Ollama API.
	/// </summary>
	[LLMClient]
	public class OllamaClient : LLMClient
	{
		/// <summary>
		/// The default base URI for the Ollama API.
		/// </summary>
		public const string DefaultBaseUri = "http://127.0.0.1:11434";

		private readonly HttpClient _http;
		private readonly OllamaEndpointConfig _endpoint;

		/// <summary>
		/// Creates a new instance of the Ollama client using the default base URI.
		/// </summary>
		[LLMClientConstructor]
		public OllamaClient()
		{
			_http = CreateHttpClient();
			_endpoint = new OllamaEndpointConfig(DefaultBaseUri);
		}

		/// <summary>
		/// Creates a new instance of the Ollama client using the specified base URI.
		/// </summary>
		/// <param name="baseUri">The base URI of the Ollama server.</param>
		public OllamaClient(string baseUri)
		{
			_http = CreateHttpClient();
			_endpoint = new OllamaEndpointConfig(baseUri ?? throw new ArgumentNullException(nameof(baseUri)));
		}

		/// <summary>
		/// Creates a new instance of the Ollama client using the specified endpoint configuration.
		/// </summary>
		/// <param name="endpointConfig">The endpoint configuration for the Ollama client.</param>
		public OllamaClient(OllamaEndpointConfig endpointConfig)
		{
			_http = CreateHttpClient();
			_endpoint = endpointConfig ?? throw new ArgumentNullException(nameof(endpointConfig));
		}

		public override string Name => "ollama";
		public override string DisplayName => "Ollama";

		public override LLMCapabilities Capabilities =>
			LLMCapabilities.ChatCompletions | LLMCapabilities.SuffixCompletions | LLMCapabilities.Embeddings |
			LLMCapabilities.Reasoning | LLMCapabilities.ToolSupport | LLMCapabilities.Vision | LLMCapabilities.StreamingCompletions;

		public override OutputFormatSupportSet SupportedOutputFormats =>
			OutputFormatSupportSet.TextWithJsonSchema;

		protected override async Task<LLModelDescriptor[]> ListModelsOverrideAsync(CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(_endpoint.ListModels))
				return Array.Empty<LLModelDescriptor>();

			var response = await RequestUtility.GetResponseAsync<JObject>(
				RequestType.Get, _endpoint.ListModels, null, cancellationToken: cancellationToken);

			var result = new List<LLModelDescriptor>();

			var models = response["models"] as JArray;
			foreach (var model in models)
			{
				var name = model["name"]?.Value<string>();
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

		private Version version = null;

		/// <summary>
		/// Gets the version of the Ollama server.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token used to cancel the request.</param>
		/// <returns>The version of the Ollama server.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the EndpointConfig is not of type <see cref="OllamaEndpointConfig"/>.</exception>
		public async Task<Version> GetVersionAsync(CancellationToken cancellationToken = default)
		{
			if (version != null)
				return version;

			var response = await RequestUtility.GetResponseAsync<JObject>(
				RequestType.Get, _endpoint.GetVersion, null, _http, cancellationToken: CancellationToken.None);

			var versionStr = response["version"]?.Value<string>();
			if (versionStr == null)
				throw new InvalidOperationException("Could not get version from response");

			version = Version.Parse(versionStr);
			return version;
		}

		protected override async Task<ChatCompletionResult> CreateChatCompletionsOverrideAsync(
			LLModelDescriptor model,
			IEnumerable<IMessage> messages,
			int count,
			IEnumerable<CompletionProperty> properties,
			OutputFormatDefinition outputFormatDefinition,
			IEnumerable<ITool> tools,
			CancellationToken cancellationToken)
		{
			await GetVersionAsync(cancellationToken);

			var body = BuildChatRequestBody(model, messages, false, properties, outputFormatDefinition, tools);

			var response = await RequestUtility.GetResponseAsync<JObject>(
				RequestType.Post,
				_endpoint.GenerateChatCompletion,
				body, _http, cancellationToken: cancellationToken);

			bool gettingThinking = false;
			var delta = ParseChatResponse(response, model, tools, false, ref gettingThinking).Delta;

			return new ChatCompletionResult(this, model, new AssistantMessage(delta.DeltaContent, delta.DeltaReasoningContent, delta.NewToolCalls));
		}

		protected override async Task<PartialChatCompletionResult> CreateStreamingChatCompletionsOverrideAsync(
			LLModelDescriptor model,
			IEnumerable<IMessage> messages,
			int count,
			IEnumerable<CompletionProperty> properties,
			OutputFormatDefinition outputFormatDefinition,
			IEnumerable<ITool> tools,
			CancellationToken cancellationToken)
		{
			await GetVersionAsync(cancellationToken);

			var body = BuildChatRequestBody(model, messages, true, properties, outputFormatDefinition, tools);

			string bodyStr = body.ToString();
			Log.Information("Request body: {0}", bodyStr);

			bool gettingThinking = false;
			var message = new PartialAssistantMessage();

			void OnStreamResponse(JObject response)
			{
				var parsed = ParseChatResponse(response, model, tools, false, ref gettingThinking);
				message.Add(parsed.Delta);

				if (parsed.Done)
					message.Complete();
			}

			// Damn Visual Studio warnings...
			var _ = RequestUtility.ProcessStreamingJsonResponseAsync<JObject>(
				RequestType.Post,
				_endpoint.GenerateChatCompletion,
				body, OnStreamResponse, _http, cancellationToken: cancellationToken)
				.ContinueWith(t =>
				{
					if (t.IsFaulted)
						message.Fail(t.Exception);
					else if (t.IsCanceled)
						message.Cancel();
				});

			return new PartialChatCompletionResult(this, model, message);
		}

		protected override async Task<CompletionResult> CreateCompletionsOverrideAsync(
			LLModelDescriptor model,
			string prompt,
			string suffix,
			int count,
			IEnumerable<CompletionProperty> properties,
			CancellationToken cancellationToken)
		{
			await GetVersionAsync(cancellationToken);

			var body = BuildRequestBody(model, prompt, suffix, false, properties);

			var response = await RequestUtility.GetResponseAsync<JObject>(
				RequestType.Post,
				_endpoint.GenerateCompletion,
				body, _http, cancellationToken: cancellationToken);

			var delta = ParseResponse(response, model, false).Delta;

			return new CompletionResult(this, model, new Completion(delta.DeltaContent));
		}

		protected override async Task<PartialCompletionResult> CreateStreamingCompletionsOverrideAsync(
			LLModelDescriptor model,
			string prompt,
			string suffix,
			int count,
			IEnumerable<CompletionProperty> properties,
			CancellationToken cancellationToken)
		{
			await GetVersionAsync(cancellationToken);

			var body = BuildRequestBody(model, prompt, suffix, true, properties);

			string bodyStr = body.ToString();
			Log.Information("Request body: {0}", bodyStr);

			var completion = new PartialCompletion();

			void OnStreamResponse(JObject response)
			{
				var parsed = ParseResponse(response, model, false);
				var delta = parsed.Delta;
				completion.Add(delta);

				if (parsed.Done)
					completion.Complete();
			}

			// Damn Visual Studio warnings...
			var _ = RequestUtility.ProcessStreamingJsonResponseAsync<JObject>(
				RequestType.Post,
				_endpoint.GenerateChatCompletion,
				body, OnStreamResponse, _http, cancellationToken: cancellationToken)
				.ContinueWith(t =>
				{
					if (t.IsFaulted)
						completion.Fail(t.Exception);
					else if (t.IsCanceled)
						completion.Cancel();
				});

			return new PartialCompletionResult(this, model, completion);
		}

		private JObject BuildRequestBody(LLModelDescriptor model, string prompt, string suffix,
			bool stream, IEnumerable<CompletionProperty> _properties)
		{
			var properties = _properties as List<CompletionProperty> ?? _properties.ToList();

			var result = new JObject
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

		private (CompletionDelta Delta, bool Done) ParseResponse(JObject response, LLModelDescriptor model, bool stream)
		{
			var content = response["response"]?.Value<string>();

			var done = response["done"]?.Value<bool>() ?? false;
			if (done)
				AppendUsage(response, model);

			return (new CompletionDelta(content), done);
		}

		private const string ThinkingTagOpen = "<think>";
		private const string ThinkingTagClose = "</think>";

		protected virtual (string content, string reasoningContent) ParseMessageContent(
			JObject message, LLModelDescriptor model, bool stream, ref bool gettingThinking)
		{
			var content = message["content"]?.Value<string>() ?? string.Empty;
			var reasoningContent = string.Empty;

			if (model.Capabilities.IsReasoning())
			{
				if (version >= new Version(0, 9, 0))
				{
					// Ollama 0.9.x and later sends the explicit reasoning content in the message

					reasoningContent = message["thinking"]?.Value<string>() ?? string.Empty;
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
			JArray toolCalls, LLModelDescriptor model, IEnumerable<ITool> tools)
		{
			List<IToolCall> result = new List<IToolCall>(toolCalls.Count);
			foreach (var toolCall in toolCalls)
			{
				var function = toolCall["function"] as JObject;
				if (function != null)
				{
					var name = function["name"]?.Value<string>() ?? string.Empty;
					var tool = tools.FirstOrDefault(t => t.Name == name);
					if (tool is FunctionTool functionTool)
					{
						var call = new FunctionToolCall(ToolCallId.Generate(0), name, function["arguments"]);
						result.Add(call);
					}
				}
			}
			return result;
		}

		protected virtual void AppendUsage(JObject response, LLModelDescriptor model)
		{
			var promptEvalCount = response["prompt_eval_count"]?.Value<int>() ?? -1;
			var evalCount = response["eval_count"]?.Value<int>() ?? -1;

			if (promptEvalCount != -1 && evalCount != -1)
				TokenUsageStatsCollector.AppendUsage(Name, model.Name, promptEvalCount, evalCount);
		}

		protected virtual (AssistantMessageDelta Delta, bool Done) ParseChatResponse(
			JObject response, LLModelDescriptor model, IEnumerable<ITool> tools, bool stream, ref bool gettingThinking)
		{
			var message = response["message"] as JObject;
			if (message == null)
				throw new LLMException("Could not get message from response.", model);

			var done = response["done"]?.Value<bool>() ?? false;
			if (done)
				AppendUsage(response, model);

			var (content, reasoningContent) = ParseMessageContent(message, model, stream, ref gettingThinking);

			var toolCalls = message["tool_calls"] as JArray;
			IEnumerable<IToolCall> toolCallsParsed = Enumerable.Empty<IToolCall>();
			if (toolCalls != null)
				toolCallsParsed = ParseToolCalls(toolCalls, model, tools);

			return (new AssistantMessageDelta(content, reasoningContent, toolCallsParsed), done);
		}

		protected virtual JObject BuildChatRequestBody(
			LLModelDescriptor model,
			IEnumerable<IMessage> _messages,
			bool stream,
			IEnumerable<CompletionProperty> _properties,
			OutputFormatDefinition outputFormatDefinition,
			IEnumerable<ITool> tools)
		{
			var messages = _messages as List<IMessage> ?? _messages.ToList();
			var builtMessages = new List<JObject>(messages.Count);
			int c = 0, lastIndex = messages.Count - 1;
			var properties = _properties as List<CompletionProperty> ?? _properties.ToList();

			foreach (var message in messages)
			{
				builtMessages.Add(BuildMessage(message, c == lastIndex));
				c++;
			}

			var result = new JObject
			{
				["model"] = model.Name,
				["messages"] = new JArray(builtMessages),
				["stream"] = stream,
				["options"] = BuildOptions(properties)
			};

			PopulateBodyWithProperties(result, model, properties, outputFormatDefinition);

			if (tools.Any())
			{
				result["tools"] = new JArray(tools.Select(BuildTool));
			}

			// The 0.9.0 version of Ollama introduced explicit thinking support (no need to parse <think> tags)
			if (version >= new Version(0, 9, 0))
			{
				if (model.Capabilities.IsReasoning() || model.Capabilities.IsUnknown())
					result["think"] = properties.OfType<ThinkProperty>().FirstOrDefault()?.Value ?? true;
			}

			return result;
		}

		protected virtual JObject BuildOptions(List<CompletionProperty> properties)
		{
			var body = new JObject();

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

				body.Add(propertyName, JToken.FromObject(value));
			}

			return body;
		}

		protected virtual void PopulateBodyWithProperties(JObject body,
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
							["arguments"] = JObject.FromObject(functionCall.Args)
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
	}
}