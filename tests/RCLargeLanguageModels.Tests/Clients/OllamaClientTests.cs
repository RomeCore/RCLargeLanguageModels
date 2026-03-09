using System.ComponentModel;
using System.Net;
using RCLargeLanguageModels.Clients.Ollama;
using RCLargeLanguageModels.Completions.Properties;
using RCLargeLanguageModels.Json.Schema;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tests.Helpers;
using RCLargeLanguageModels.Tools;

namespace RCLargeLanguageModels.Tests.Clients
{
	public class OllamaClientTests
	{
		#region Chat

		[Fact]
		public async Task CreateChatCompletion_SendsCorrectRequest()
		{
			var http = HttpHelper.MakeClient((uri, headers, body) =>
			{
				Assert.Equal("http://localhost:11434/api/chat", uri.AbsoluteUri);

				var bodyStr = body.ToJsonString();
				Assert.Contains("\"model\":\"llama3\"", bodyStr);
				Assert.Contains("\"role\":\"user\"", bodyStr);
				Assert.Contains("\"content\":\"Hi\"", bodyStr);

				return (HttpStatusCode.OK, """
					{
						"message": {
							"role": "assistant",
							"content": "Hello!"
						},
						"done": true
					}
					""");
			});

			var client = new OllamaClient(
				"http://localhost:11434",
				http: http,
				serverVersion: new Version("15.0.0"));

			var model = new LLModel(client, "llama3");

			var result = await model.ChatAsync(
				[new UserMessage("Hi")],
				cancellationToken: TestContext.Current.CancellationToken);

			Assert.Single(result.Choices);
			Assert.Equal("Hello!", result.Choices[0].Content);
		}


		[Fact]
		public async Task CreateChatCompletion_SerializesTools()
		{
			[Name("test_function")]
			[Description("A test function.")]
			ToolResult CallFunction(string input)
			{
				return new ToolResult();
			}

			var tool = FunctionTool.From(CallFunction);

			var http = HttpHelper.MakeClient((uri, headers, body) =>
			{
				var bodyStr = body.ToJsonString();
				Assert.Contains("\"tools\"", bodyStr);
				Assert.Contains("\"test_function\"", bodyStr);

				return (HttpStatusCode.OK, """
					{
						"message":{
							"role":"assistant",
							"content":"Hello!"
						},
						"done":true
					}
					""");
			});

			var client = new OllamaClient(
				"http://localhost:11434",
				http: http,
				serverVersion: new Version("15.0.0"));

			var model = new LLModel(client, "llama3")
				.WithTools(tool);

			await model.ChatAsync(
				[new UserMessage("Hi")],
				cancellationToken: TestContext.Current.CancellationToken);
		}


		[Fact]
		public async Task CreateChatCompletion_SerializesTemperatureOption()
		{
			var http = HttpHelper.MakeClient((uri, headers, body) =>
			{
				var bodyStr = body.ToJsonString();
				Assert.Contains("\"options\"", bodyStr);
				Assert.Contains("\"temperature\"", bodyStr);

				return (HttpStatusCode.OK, """
					{
						"message":{
							"role":"assistant",
							"content":"Hello!"
						},
						"done":true
					}
					""");
			});

			var client = new OllamaClient(
				"http://localhost:11434",
				http: http,
				serverVersion: new Version("15.0.0"));

			var model = new LLModel(client, "llama3")
				.WithProperties(new TemperatureProperty(0.7f));

			await model.ChatAsync(
				[new UserMessage("Hi")],
				cancellationToken: TestContext.Current.CancellationToken);
		}

		#endregion


		#region Generate

		[Fact]
		public async Task CreateCompletion_SendsCorrectRequest()
		{
			var http = HttpHelper.MakeClient((uri, headers, body) =>
			{
				Assert.Equal("http://localhost:11434/api/generate", uri.AbsoluteUri);

				var bodyStr = body.ToJsonString();
				Assert.Contains("\"prompt\"", bodyStr);
				Assert.Contains("Once upon a time", bodyStr);

				return (HttpStatusCode.OK, """
			{
				"response":"Hello!",
				"done":true
			}
			""");
			});

			var client = new OllamaClient(
				"http://localhost:11434",
				http: http,
				serverVersion: new Version("15.0.0"));

			var model = new LLModel(client, "llama3");

			var result = await model.CompleteAsync(
				"Once upon a time",
				cancellationToken: TestContext.Current.CancellationToken);

			Assert.Single(result.Choices);
			Assert.Equal("Hello!", result.Choices[0].Content);
		}


		[Fact]
		public async Task CreateCompletion_SerializesTemperatureOption()
		{
			var http = HttpHelper.MakeClient((uri, headers, body) =>
			{
				var bodyStr = body.ToJsonString();
				Assert.Contains("\"options\"", bodyStr);
				Assert.Contains("\"temperature\"", bodyStr);

				return (HttpStatusCode.OK, """
					{
						"response":"Hello!",
						"done":true
					}
					""");
			});

			var client = new OllamaClient(
				"http://localhost:11434",
				http: http,
				serverVersion: new Version("15.0.0"));

			var model = new LLModel(client, "llama3")
				.WithProperties(new TemperatureProperty(0.8f));

			await model.CompleteAsync(
				"Test",
				cancellationToken: TestContext.Current.CancellationToken);
		}

		#endregion


		#region Embeddings

		[Fact]
		public async Task CreateEmbeddings_SendsCorrectRequest()
		{
			var http = HttpHelper.MakeClient((uri, headers, body) =>
			{
				Assert.Equal("http://localhost:11434/api/embed", uri.AbsoluteUri);

				var bodyStr = body.ToJsonString();
				Assert.Contains("\"input\"", bodyStr);

				return (HttpStatusCode.OK, """
					{
						"embedding":[
							0.1,
							0.2,
							0.3
						]
					}
					""");
			});

			var client = new OllamaClient(
				"http://localhost:11434",
				http: http,
				serverVersion: new Version("15.0.0"));

			var model = new LLModel(client, "llama3");

			var result = await model.EmbedAsync(
				"test",
				cancellationToken: TestContext.Current.CancellationToken);

			Assert.Single(result.Embeddings);
			Assert.Equal([0.1f, 0.2f, 0.3f], result.Embeddings[0]);
		}

		#endregion
	}
}