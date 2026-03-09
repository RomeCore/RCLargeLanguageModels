using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text.Json.Nodes;
using RCLargeLanguageModels.Clients.OpenAI;
using RCLargeLanguageModels.Completions.Properties;
using RCLargeLanguageModels.Json.Schema;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tests.Helpers;
using RCLargeLanguageModels.Tools;

namespace RCLargeLanguageModels.Tests.Clients
{
	#nullable disable

	public class OpenAIComplatibleClientTests
	{
		#region Chat Completions

		[Fact]
		public async Task CreateChatCompletion_SendsCorrectRequest()
		{
			var http = HttpHelper.MakeClient((uri, headers, body) =>
			{
				return (HttpStatusCode.OK, """
					{
						"choices":[
							{
								"message":{
									"content": "Hello!"
								}
							}
						]
					}
					""");
			});

			var client = new OpenAICompatibleClient(
				"https://api.test.com/v1",
				"test-key",
				http);
			var model = new LLModel(client, "gpt-test");

			var messages = new IMessage[]
			{
				new UserMessage("Hi")
			};

			// Act
			var result = await model.ChatAsync(
				messages,
				cancellationToken: TestContext.Current.CancellationToken);

			// Assert
			Assert.Single(result.Choices);
			Assert.Equal("Hello!", result.Choices[0].Content);
		}

		[Fact]
		public async Task CreateChatCompletion_SendsCorrectEndpointHeadersAndBody()
		{
			var http = HttpHelper.MakeClient((uri, headers, body) =>
			{
				Assert.Equal("https://api.test.com/v1/chat/completions", uri.AbsoluteUri);

				Assert.True(headers.ContainsKey("Authorization"));
				Assert.Equal("Bearer test-key", headers["Authorization"]);

				var bodyStr = body.ToJsonString();
				Assert.Contains("\"model\":\"gpt-test\"", bodyStr);
				Assert.Contains("\"role\":\"user\"", bodyStr);
				Assert.Contains("\"content\":\"Hi\"", bodyStr);

				return (HttpStatusCode.OK, """
					{
						"choices":[
							{
								"message":{
									"content":"Hello!"
								}
							}
						]
					}
					""");
			});

			var client = new OpenAICompatibleClient(
				"https://api.test.com/v1",
				"test-key",
				http);

			var model = new LLModel(client, "gpt-test");

			await model.ChatAsync(
				[new UserMessage("Hi")],
				cancellationToken: TestContext.Current.CancellationToken);
		}

		[Fact]
		public async Task CreateChatCompletion_ParsesMultipleChoices()
		{
			var http = HttpHelper.MakeClient((uri, headers, body) =>
			{
				return (HttpStatusCode.OK, """
				{
					"choices":[
						{
							"message":{"content":"Hello"}
						},
						{
							"message":{"content":"Hi there"}
						}
					]
				}
				""");
			});

			var client = new OpenAICompatibleClient(
				"https://api.test.com/v1",
				"test-key",
				http);

			var model = new LLModel(client, "gpt-test");

			var result = await model.ChatAsync(
				[new UserMessage("Hi")],
				cancellationToken: TestContext.Current.CancellationToken);

			Assert.Equal(2, result.Choices.Length);
			Assert.Equal("Hello", result.Choices[0].Content);
			Assert.Equal("Hi there", result.Choices[1].Content);
		}

		[Fact]
		public async Task CreateChatCompletion_SerializesTools()
		{
			[Name("test_function")]
			[Description("A test function for LLM.")]
			ToolResult CallFunction([Description("Input to provide to tool")] string input)
			{
				return new ToolResult();
			}
			var tool = FunctionTool.From(CallFunction);

			var http = HttpHelper.MakeClient((uri, headers, body) =>
			{
				var bodyStr = body.ToJsonString();
				Assert.Contains("\"tools\"", bodyStr);
				Assert.Contains("\"test_function\"", bodyStr);
				Assert.Contains("\"input\"", bodyStr);

				return (HttpStatusCode.OK, """
					{
						"choices":[
							{
								"message":{"content":"Hello!"}
							}
						]
					}
					""");
			});

			var client = new OpenAICompatibleClient(
				"https://api.test.com/v1",
				"test-key",
				http);

			var model = new LLModel(client, "gpt-test")
				.WithTools(tool);

			await model.ChatAsync(
				[new UserMessage("Hi")],
				cancellationToken: TestContext.Current.CancellationToken);
		}

		[Fact]
		public async Task CreateChatCompletion_SerializesTemperatureProperty()
		{
			var http = HttpHelper.MakeClient((uri, headers, body) =>
			{
				var bodyStr = body.ToJsonString();
				Assert.Contains("\"temperature\":2", bodyStr);
				// 1.0 mapped from [0..1] → [-2..2]

				return (HttpStatusCode.OK, """
					{
						"choices":[
							{
								"message":{"content":"Hello!"}
							}
						]
					}
					""");
			});

			var client = new OpenAICompatibleClient(
				"https://api.test.com/v1",
				"test-key",
				http);

			var model = new LLModel(client, "gpt-test")
				.WithProperties(new TemperatureProperty(1.0f));

			await model.ChatAsync(
				[new UserMessage("Hi")],
				cancellationToken: TestContext.Current.CancellationToken);
		}

		#endregion

		#region Completions

		[Fact]
		public async Task CreateCompletion_SendsCorrectRequest()
		{
			var http = HttpHelper.MakeClient((uri, headers, body) =>
			{
				return (HttpStatusCode.OK, """
					{
						"choices":[
							{
								"text": "Hello!"
							}
						]
					}
					""");
			});

			var client = new OpenAICompatibleClient(
				"https://api.test.com/v1",
				"test-key",
				http);
			var model = new LLModel(client, "gpt-test");

			// Act
			var result = await model.CompleteAsync(
				"Once upon a time, our hero says...",
				cancellationToken: TestContext.Current.CancellationToken);

			// Assert
			Assert.Single(result.Choices);
			Assert.Equal("Hello!", result.Choices[0].Content);
		}

		[Fact]
		public async Task CreateCompletion_UsesCorrectEndpoint()
		{
			var http = HttpHelper.MakeClient((uri, headers, body) =>
			{
				Assert.Equal("https://api.test.com/v1/completions", uri.AbsoluteUri);

				var bodyStr = body.ToJsonString();
				Assert.Contains("\"prompt\"", bodyStr);
				Assert.Contains("Once upon a time", bodyStr);

				return (HttpStatusCode.OK, """
					{
						"choices":[
							{"text":"Hello!"}
						]
					}
					""");
			});

			var client = new OpenAICompatibleClient(
				"https://api.test.com/v1",
				"test-key",
				http);

			var model = new LLModel(client, "gpt-test");

			await model.CompleteAsync(
				"Once upon a time",
				cancellationToken: TestContext.Current.CancellationToken);
		}

		[Fact]
		public async Task CreateCompletion_ParsesMultipleChoices()
		{
			var http = HttpHelper.MakeClient((uri, headers, body) =>
			{
				return (HttpStatusCode.OK, """
					{
						"choices":[
							{"text":"Hello"},
							{"text":"Hi"}
						]
					}
					""");
			});

			var client = new OpenAICompatibleClient(
				"https://api.test.com/v1",
				"test-key",
				http);

			var model = new LLModel(client, "gpt-test");

			var result = await model.CompleteAsync(
				"Test",
				cancellationToken: TestContext.Current.CancellationToken);

			Assert.Equal(2, result.Choices.Length);
			Assert.Equal("Hello", result.Choices[0].Content);
			Assert.Equal("Hi", result.Choices[1].Content);
		}

		#endregion

		#region Embeddings

		[Fact]
		public async Task CreateEmbeddings_SendsCorrectRequest()
		{
			var http = HttpHelper.MakeClient((uri, headers, body) =>
			{
				return (HttpStatusCode.OK, """
					{
					  "object": "list",
					  "data": [
					    {
					      "object": "embedding",
					      "embedding": [
					        0.0023064255,
					        -0.009327292,
					        -0.0028842222
					      ],
					      "index": 0
					    }
					  ],
					  "model": "gpt-test",
					  "usage": {
					    "prompt_tokens": 8,
					    "total_tokens": 8
					  }
					}
					""");
			});

			var client = new OpenAICompatibleClient(
				"https://api.test.com/v1",
				"test-key",
				http);
			var model = new LLModel(client, "gpt-test");

			// Act
			var result = await model.EmbedAsync(
				"Once upon a time, our hero says...",
				cancellationToken: TestContext.Current.CancellationToken);

			// Assert
			Assert.Single(result.Embeddings);
			Assert.Equal([ 0.0023064255f, -0.009327292f, -0.0028842222f ], result.Embeddings[0]);
		}

		[Fact]
		public async Task CreateEmbeddings_SendsCorrectRequestBody()
		{
			var http = HttpHelper.MakeClient((uri, headers, body) =>
			{
				Assert.Equal("https://api.test.com/v1/embeddings", uri.AbsoluteUri);

				var bodyStr = body.ToJsonString();
				Assert.Contains("\"input\"", bodyStr);
				Assert.Contains("Once upon a time", bodyStr);

				return (HttpStatusCode.OK, """
					{
						"data":[
							{
								"embedding":[1,2,3],
								"index":0
							}
						]
					}
					""");
			});

			var client = new OpenAICompatibleClient(
				"https://api.test.com/v1",
				"test-key",
				http);

			var model = new LLModel(client, "gpt-test");

			await model.EmbedAsync(
				"Once upon a time",
				cancellationToken: TestContext.Current.CancellationToken);
		}

		[Fact]
		public async Task CreateEmbeddings_ParsesMultipleEmbeddings()
		{
			var http = HttpHelper.MakeClient((uri, headers, body) =>
			{
				return (HttpStatusCode.OK, """
					{
						"data":[
							{
								"embedding":[1,2,3],
								"index":0
							},
							{
								"embedding":[4,5,6],
								"index":1
							}
						]
					}
					""");
			});

			var client = new OpenAICompatibleClient(
				"https://api.test.com/v1",
				"test-key",
				http);

			var model = new LLModel(client, "gpt-test");

			var result = await model.EmbedAsync(
				"test",
				cancellationToken: TestContext.Current.CancellationToken);

			Assert.Equal(2, result.Embeddings.Length);

			Assert.Equal([1f, 2f, 3f], result.Embeddings[0]);
			Assert.Equal([4f, 5f, 6f], result.Embeddings[1]);
		}

		#endregion
	}
}