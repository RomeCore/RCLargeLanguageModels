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
using RCLargeLanguageModels.Embeddings;
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
		protected override async Task<EmbeddingResult> CreateEmbeddingsOverrideAsync(LLModelDescriptor model, IEnumerable<string> inputs, IEnumerable<CompletionProperty>? properties, CancellationToken cancellationToken)
		{
			var body = BuildEmbeddingRequestBody(model, inputs, properties);
			var headers = GetRequestHeaders();

			var response = await RequestUtility.GetResponseAsync(
				RequestType.Post,
				_endpoint.GenerateEmbedding,
				body,
				_http,
				headers,
				cancellationToken);

			response.EnsureSuccessStatusCode();
			var responseContent = await response.ParseContentAsync<JsonObject>(cancellationToken);

			if (responseContent["data"] is not JsonArray data || data.Count == 0)
				throw new InvalidDataException("No embedding data in response.");

			List<Embedding> embeddings = new List<Embedding>();

			foreach (var item in data)
			{
				int index = item!["index"]?.GetValue<int>() ?? -1;

				var embeddingObj = item["embedding"] as JsonArray;
				if (embeddingObj == null)
					throw new InvalidDataException("Missing or invalid embedding vector in response.");

				var vector = new List<float>();
				foreach (var value in embeddingObj)
				{
					vector.Add(value!.GetValue<float>());
				}

				embeddings.Add(new Embedding(vector, model));
			}

			var metadata = new List<IMetadata>();
			if (responseContent["usage"] is JsonObject usage)
				metadata.Add(GetUsageMetadata(usage));

			return new EmbeddingResult(this, model, embeddings, metadata);
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
	}
}