using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit.Sdk;

namespace RCLargeLanguageModels.Tests.Helpers
{
	public static class HttpHelper
	{
		private class HttpHandler : HttpMessageHandler
		{
			private readonly Func<Uri /*URI*/, Dictionary<string, string> /*Headers*/, JsonNode /*Body*/, (HttpStatusCode, string)> _handler;

			public HttpHandler(Func<Uri, Dictionary<string, string>, JsonNode, (HttpStatusCode, string)> handler)
			{
				_handler = handler;
			}

			protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				try
				{
					var uri = request.RequestUri!;
					var headers = request.Headers.ToDictionary(x => x.Key, x => x.Value.First());
					var body = JsonNode.Parse(request.Content!.ReadAsStringAsync(cancellationToken).Result)!;

					TestContext.Current.TestOutputHelper!.WriteLine($"""
						REQUEST URI: {uri}

						HEADERS
						{(headers.Count == 0 ?
							"<empty>" :
							string.Join(", ", headers.Select(x => $"{x.Key}: {x.Value}")))}

						BODY
						{body.ToJsonString(new JsonSerializerOptions { WriteIndented = true })}
						""");

					var (status, content) = _handler(uri, headers, body);

					var response = new HttpResponseMessage(status);
					response.Content = new StringContent(content);
					return Task.FromResult(response);
				}
				catch (XunitException ex)
				{
					Assert.Fail(ex.Message);
					throw;
				}
			}
		}

		/// <summary>
		/// Makes a HTTP client for testing purposes.
		/// </summary>
		public static HttpClient MakeClient(Func<Uri, Dictionary<string, string>, JsonNode, (HttpStatusCode, string)> handler)
		{
			return new HttpClient(new HttpHandler(handler));
		}

		/// <summary>
		/// Makes a HTTP client for testing purposes.
		/// </summary>
		public static HttpClient MakeClient(Func<Uri, Dictionary<string, string>, JsonNode, string> handler)
		{
			return new HttpClient(new HttpHandler((uri, headers, body) => (HttpStatusCode.OK, handler(uri, headers, body))));
		}

		/// <summary>
		/// Makes a HTTP client for testing purposes.
		/// </summary>
		public static HttpClient MakeClient(Func<Uri, JsonNode, (HttpStatusCode, string)> handler)
		{
			return new HttpClient(new HttpHandler((uri, headers, body) => handler(uri, body)));
		}

		/// <summary>
		/// Makes a HTTP client for testing purposes.
		/// </summary>
		public static HttpClient MakeClient(Func<Uri, JsonNode, string> handler)
		{
			return new HttpClient(new HttpHandler((uri, headers, body) => (HttpStatusCode.OK, handler(uri, body))));
		}

		/// <summary>
		/// Makes a HTTP client for testing purposes.
		/// </summary>
		public static HttpClient MakeClient(Func<JsonNode, (HttpStatusCode, string)> handler)
		{
			return new HttpClient(new HttpHandler((uri, headers, body) => handler(body)));
		}

		/// <summary>
		/// Makes a HTTP client for testing purposes.
		/// </summary>
		public static HttpClient MakeClient(Func<JsonNode, string> handler)
		{
			return new HttpClient(new HttpHandler((uri, headers, body) => (HttpStatusCode.OK, handler(body))));
		}

		/// <summary>
		/// Makes a HTTP client for testing purposes.
		/// </summary>
		public static HttpClient MakeClient(Func<(HttpStatusCode, string)> handler)
		{
			return new HttpClient(new HttpHandler((uri, headers, body) => handler()));
		}

		/// <summary>
		/// Makes a HTTP client for testing purposes.
		/// </summary>
		public static HttpClient MakeClient(Func<string> handler)
		{
			return new HttpClient(new HttpHandler((uri, headers, body) => (HttpStatusCode.OK, handler())));
		}
	}
}