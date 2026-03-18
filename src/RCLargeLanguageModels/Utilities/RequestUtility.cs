using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Utilities
{
	/// <summary>
	/// Represents the type of HTTP request methods supported.
	/// </summary>
	public enum RequestType
	{
		/// <summary>
		/// Corresponds to the HTTP POST method.
		/// </summary>
		Post,

		/// <summary>
		/// Corresponds to the HTTP GET method.
		/// </summary>
		Get,

		/// <summary>
		/// Corresponds to the HTTP PUT method.
		/// </summary>
		Put,

		/// <summary>
		/// Corresponds to the HTTP DELETE method.
		/// </summary>
		Delete
	}

	/// <summary>
	/// A static utility class for making HTTP requests and handling JSON responses.
	/// </summary>
	public static class RequestUtility
	{
		/// <summary>
		/// Gets the shared HttpClient instance configured with default headers.
		/// </summary>
		public static HttpClient Client { get; }

		static RequestUtility()
		{
			Client = new HttpClient();

			Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		}

		/// <summary>
		/// Sends an HTTP request and returns the response as a JSON token.
		/// </summary>
		/// <param name="requestType">The type of HTTP request (Post, Get, Put, Delete).</param>
		/// <param name="uri">The target URI for the request.</param>
		/// <param name="body">The object to serialize as the JSON body of the request. Can be null for bodyless requests.</param>
		/// <param name="client">A <see cref="HttpClient"/> to use for requesting. Can be null</param>
		/// <param name="headers">Optional dictionary of headers to include in the request.</param>
		/// <returns>A <see cref="JsonNode"/> representing the JSON response.</returns>
		public static async Task<JsonNode> GetJsonResponseAsync(RequestType requestType, string uri,
			object? body, HttpClient? client = null, Dictionary<string, string>? headers = null)
		{
			HttpResponseMessage response = await GetResponseAsync(requestType, uri, body, client, headers);
			string responseContent = await response.Content.ReadAsStringAsync();
			return JsonNode.Parse(responseContent)!;
		}

		/// <summary>
		/// Sends an HTTP request and returns the deserialized response as the specified type.
		/// </summary>
		/// <typeparam name="T">The type to deserialize the response into.</typeparam>
		/// <param name="requestType">The type of HTTP request (Post, Get, Put, Delete).</param>
		/// <param name="uri">The target URI for the request.</param>
		/// <param name="body">The object to serialize as the JSON body of the request. Can be null for bodyless requests.</param>
		/// <param name="client">A <see cref="HttpClient"/> to use for requesting. Can be null</param>
		/// <param name="headers">Optional dictionary of headers to include in the request.</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the request.</param>
		/// <returns>The deserialized response of type <typeparamref name="T"/>.</returns>
		public static async Task<T> GetResponseAsync<T>(RequestType requestType, string uri, object? body,
			HttpClient? client = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
		{
			HttpResponseMessage response = await GetResponseAsync(requestType, uri, body, client, headers, cancellationToken);
			response.EnsureSuccessStatusCode();
			string responseContent = await response.Content.ReadAsStringAsync();
			return JsonSerializer.Deserialize<T>(responseContent)!;
		}

		/// <summary>
		/// Sends an HTTP request and returns the raw <see cref="HttpResponseMessage"/>.
		/// </summary>
		/// <param name="requestType">The type of HTTP request (Post, Get, Put, Delete).</param>
		/// <param name="uri">The target URI for the request.</param>
		/// <param name="body">The object to serialize as the JSON body of the request. Can be null for bodyless requests.</param>
		/// <param name="client">A <see cref="HttpClient"/>. Can be null</param>
		/// <param name="headers">Optional dictionary of headers to include in the request.</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the request.</param>
		/// <returns>The <see cref="HttpResponseMessage"/> from the request.</returns>
		public static async Task<HttpResponseMessage> GetResponseAsync(RequestType requestType,
			string uri, object? body, HttpClient? client = null, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
		{
			HttpRequestMessage request = new HttpRequestMessage(GetHttpMethod(requestType), uri);

			if (body != null)
			{
				string jsonContent = JsonSerializer.Serialize(body);
				request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
			}

			if (headers != null)
			{
				foreach (var header in headers)
				{
					request.Headers.Add(header.Key, header.Value);
				}
			}

			if (client == null)
				client = Client;

			var result = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
			return result;
		}

		/// <summary>
		/// Processes a streaming JSON response and calls a callback function for each parsed object.
		/// </summary>
		/// <param name="requestType">HTTP request type</param>
		/// <param name="uri">Target endpoint</param>
		/// <param name="body">Request body</param>
		/// <param name="onDataReceived">Callback for parsed objects</param>
		/// <param name="client">A <see cref="HttpClient"/>. Can be null</param>
		/// <param name="headers">Request headers</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns>Task representing the streaming operation</returns>
		/// <remarks>
		/// This method expects newline-delimited JSON (NDJSON) or similar streaming format.
		/// </remarks>
		public static async Task ProcessStreamingJsonResponseAsync<T>(
			RequestType requestType,
			string uri,
			object body,
			Action<T> onDataReceived,
			HttpClient? client = null,
			Dictionary<string, string>? headers = null,
			CancellationToken cancellationToken = default)
		{
			void OnDataReceived(JsonNode token)
			{
				var obj = JsonSerializer.Deserialize<T>(token)!;
				onDataReceived(obj);
			}

			await ProcessStreamingJsonResponseAsync(requestType, uri, body, OnDataReceived,
				client, headers, cancellationToken);
		}

		/// <summary>
		/// Processes a streaming JSON response
		/// </summary>
		/// <param name="requestType">HTTP request type</param>
		/// <param name="uri">Target endpoint</param>
		/// <param name="body">Request body</param>
		/// <param name="onDataReceived">Callback for parsed JSON objects</param>
		/// <param name="client">A <see cref="HttpClient"/>. Can be null</param>
		/// <param name="headers">Request headers</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns>Task representing the streaming operation</returns>
		/// <remarks>
		/// This method expects newline-delimited JSON (NDJSON) or similar streaming format.
		/// </remarks>
		public static Task ProcessStreamingJsonResponseAsync(
			RequestType requestType,
			string uri,
			object body,
			Action<JsonNode> onDataReceived,
			HttpClient? client = null,
			Dictionary<string, string>? headers = null,
			CancellationToken cancellationToken = default)
		{
			return ProcessStreamingResponseAsync(requestType, uri, body, l => onDataReceived(JsonNode.Parse(l)!),
				client, headers, cancellationToken);
		}

		/// <summary>
		/// Processes a streaming response
		/// </summary>
		/// <param name="requestType">HTTP request type</param>
		/// <param name="uri">Target endpoint</param>
		/// <param name="body">Request body</param>
		/// <param name="onDataReceived">Callback for received lines</param>
		/// <param name="client">A <see cref="HttpClient"/> to use for requesting. Can be null</param>
		/// <param name="headers">Request headers</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns>Task representing the streaming operation</returns>
		/// <remarks>
		/// This method expects that response is delimited by newlines.
		/// </remarks>
		public static async Task ProcessStreamingResponseAsync(
			RequestType requestType,
			string uri,
			object body,
			Action<string> onDataReceived,
			HttpClient? client = null,
			Dictionary<string, string>? headers = null,
			CancellationToken cancellationToken = default)
		{
			var response = await GetResponseAsync(requestType, uri, body, client, headers, cancellationToken);
			response.EnsureSuccessStatusCode();

			using (var responseStream = await response.Content.ReadAsStreamAsync())
			using (var reader = new StreamReader(responseStream))
			{
				while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
				{
					var line = await reader.ReadLineAsync();
					if (!string.IsNullOrWhiteSpace(line))
					{
						// SSE handling

						if (line.StartsWith(":"))
							continue; // Ignore comments

						if (line.StartsWith("data:"))
						{
							line = line.Substring(5).Trim();
							if (line == "[DONE]")
								break;
						}

						onDataReceived(line);
					}
				}
			}
		}
		
		/// <summary>
		/// Converts a <see cref="RequestType"/> enum value to the corresponding <see cref="HttpMethod"/>.
		/// </summary>
		/// <param name="requestType">The <see cref="RequestType"/> to convert.</param>
		/// <returns>The corresponding <see cref="HttpMethod"/>.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the <paramref name="requestType"/> is not valid.</exception>
		public static HttpMethod GetHttpMethod(RequestType requestType)
		{
			switch (requestType)
			{
				case RequestType.Post:
					return HttpMethod.Post;
				case RequestType.Get:
					return HttpMethod.Get;
				case RequestType.Put:
					return HttpMethod.Put;
				case RequestType.Delete:
					return HttpMethod.Delete;
				default:
					throw new ArgumentOutOfRangeException(nameof(requestType), requestType, null);
			}
		}

		/// <summary>
		/// Parses the content of an HTTP response into a specific type.
		/// </summary>
		/// <typeparam name="T">The type to parse the response into.</typeparam>
		/// <param name="message">The HTTP response message.</param>
		/// <param name="ct">The cancellation token used to cancel the operation.</param>
		/// <returns>The parsed object of type <typeparamref name="T"/>.</returns>
		public static async Task<T> ParseContentAsync<T>(this HttpResponseMessage message, CancellationToken ct = default)
		{
#if NET6_0_OR_GREATER
			var content = await message.Content.ReadAsStringAsync(ct);
#else
			var content = await message.Content.ReadAsStringAsync();
#endif
			return JsonSerializer.Deserialize<T>(content)!;
		}
	}
}