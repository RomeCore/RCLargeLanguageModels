using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Tools;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Formats;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Utilities;
using Serilog;

namespace RCLargeLanguageModels.Clients.OpenAI
{
	public partial class OpenAICompatibleClient
	{
		protected override async Task<CompletionResult> CreateCompletionsOverrideAsync(LLModelDescriptor model,
			string prompt, string? suffix, int count, IEnumerable<CompletionProperty> properties, CancellationToken cancellationToken)
		{
			var body = BuildCompletionRequestBody(model, prompt, suffix, properties, count, false);
			var headers = GetRequestHeaders();

			var response = await RequestUtility.GetResponseAsync(RequestType.Post, _endpoint.GenerateCompletion,
				body, _http, headers, cancellationToken);
			response.EnsureSuccessStatusCode();

			var responseContent = await response.ParseContentAsync<JsonObject>(cancellationToken);
			if (responseContent["choices"] is not JsonArray choices || choices.Count == 0)
				throw new InvalidDataException("No choices in response.");

			List<Completion> results = new List<Completion>();

			foreach (var choice in choices)
			{
				results.Add(new Completion(choice!["text"]?.GetValue<string>()));
			}

			if (responseContent["usage"] is JsonObject usage)
				AppendUsage(usage, model);

			return new CompletionResult(this, model, results);
		}

		protected override Task<PartialCompletionResult> CreateStreamingCompletionsOverrideAsync(LLModelDescriptor model,
			string prompt, string? suffix, int count, IEnumerable<CompletionProperty> properties, CancellationToken cancellationToken)
		{
			var results = Enumerable.Range(0, count).Select(i => new PartialCompletion()).ToImmutableArray();

			void OnDataReceived(JsonObject data)
			{
				if (data["choices"] is not JsonArray choices || choices.Count == 0)
					throw new InvalidDataException("No choices in response.");

				foreach (var choice in choices)
				{
					int index = choice!["index"]!.GetValue<int>();
					var completion = results[index];

					if (choice["finish_reason"]?.GetValue<string>() is string finishReason)
						completion.Complete();

					if (choice["text"]?.GetValue<string>() is string delta)
						completion.Add(delta);
				}

				if (data["usage"] is JsonObject usage)
					AppendUsage(usage, model);
			}

			var body = BuildCompletionRequestBody(model, prompt, suffix, properties, count, true);
			var headers = GetRequestHeaders();

			Task.Run(() => RequestUtility.ProcessStreamingJsonResponseAsync<JsonObject>(RequestType.Post, _endpoint.GenerateCompletion,
				body, OnDataReceived, _http, headers, cancellationToken))
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

			return Task.FromResult(new PartialCompletionResult(this, model, results));
		}

		private JsonObject BuildCompletionRequestBody(LLModelDescriptor model, string prompt, string? suffix, IEnumerable<CompletionProperty> properties, int count, bool stream)
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
	}
}