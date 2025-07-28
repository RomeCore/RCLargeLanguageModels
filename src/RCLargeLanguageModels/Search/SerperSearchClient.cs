using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RCLargeLanguageModels.Json;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Messages.Attachments;
using RCLargeLanguageModels.Security;
using RCLargeLanguageModels.Tools;
using RCLargeLanguageModels.Utilities;

namespace RCLargeLanguageModels.Search
{
	public class SerperSearchRequestModel
	{
		[JsonProperty("q")]
		public string Query { get; set; }

		[JsonProperty("num")]
		public int Num { get; set; } = 10;

		[JsonProperty("start")]
		public int Start { get; set; } = 0;

		[JsonProperty("gl")]
		public string Country { get; set; } = "us";

		[JsonProperty("hl")]
		public string Language { get; set; } = "en";
	}

	[JsonObject]
	public class SerperSearchResponseModel
	{
		public class SearchParametersModel
		{
			[JsonProperty("q")]
			public string Query { get; set; }
		}

		public class AnswerBoxModel
		{
			[JsonProperty("title")]
			public string Title { get; set; }
			[JsonProperty("answer")]
			public string Answer { get; set; }
			[JsonProperty("source")]
			public string Source { get; set; }
			[JsonProperty("sourceLink")]
			public string SourceLink { get; set; }
		}

		public class SitelinkModel
		{
			[JsonProperty("title")]
			public string Title { get; set; }
			[JsonProperty("link")]
			public string Link { get; set; }
		}

		public class OrganicModel
		{
			[JsonProperty("title")]
			public string Title { get; set; }
			[JsonProperty("link")]
			public string Link { get; set; }
			[JsonProperty("snippet")]
			public string Snippet { get; set; }
			[JsonProperty("sitelinks")]
			public List<SitelinkModel> Sitelinks { get; set; } = new List<SitelinkModel>();
		}

		public class RelatedSearchModel
		{
			[JsonProperty("query")]
			public string Query { get; set; }
		}

		[JsonProperty("searchParameters")]
		public SearchParametersModel SearchParameters { get; set; }
		[JsonProperty("answerBox")]
		public AnswerBoxModel AnswerBox { get; set; }
		[JsonProperty("organic")]
		public List<OrganicModel> Organic { get; set; } = new List<OrganicModel>();
		[JsonProperty("relatedSearches")]
		public List<RelatedSearchModel> RelatedSearches { get; set; } = new List<RelatedSearchModel>();
	}

	public class SerperSearchAttachment : RawTextAttachment
	{
		public SerperSearchAttachment(SerperSearchResponseModel response)
			: base($"Serper Search \"{response.SearchParameters.Query}\"", GetTextContent(response))
		{
		}

		[JsonConstructor]
		private SerperSearchAttachment(string title, string content) : base(title, content)
		{
		}

		private static string GetTextContent(SerperSearchResponseModel response)
		{
			var serializer = JsonSerializer.Create();
			using (var writer = new StringWriter())
			{
				serializer.Serialize(writer, response);
				return writer.ToString();
			}
		}
	}

	[ContainsTools]
	public static class SerperSearchClient
	{
		/// <summary>
		/// The name of the API key token storage entry.
		/// </summary>
		public const string ApiKeyName = "serper-api-key";

		public static FunctionTool Tool { get; }

		[FunctionToolMethod("serper_web_search", "Search in the web using Serper", "chat")]
		[WriteToolDefinitionInto(nameof(Tool))]
		public static async Task<ToolResult> SearchAsync(

			[Description("The query to search for.")]
			string query,
			string countryCode,
			string languageCode,
			CancellationToken cancellationToken = default
			)
		{
			var client = new HttpClient();

			var body = new SerperSearchRequestModel
			{
				Query = query,
				Country = countryCode,
				Language = languageCode
			};

			var headers = new Dictionary<string, string>
			{
				{ "X-API-KEY", TokenStorage.GetTokenShared(ApiKeyName) }
			};

			var response = await RequestUtility.GetResponseAsync<SerperSearchResponseModel>(
				RequestType.Post, 
				"https://google.serper.dev/search",
				body, client, headers, cancellationToken);

			var result = new SerperSearchAttachment(response);
			return new ToolResult(result);
		}
	}
}