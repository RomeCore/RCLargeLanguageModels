using RCLargeLanguageModels.Clients.Ollama;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Messages;
using Xunit.Abstractions;

namespace RCLargeLanguageModels.Tests
{
	public class OllamaGenerationTests
	{
		private readonly LLMClient client;
		private readonly ITestOutputHelper output;

		public OllamaGenerationTests(ITestOutputHelper output)
		{
			this.client = new OllamaClient();
			this.output = output;
		}

		private async Task Complete(int i, string prompt, string? suffix)
		{
			output.WriteLine($"[{i}] Completing \"{prompt}\"...\"{suffix}\"\n");

			var result = await client.CreateCompletionAsync(
				"qwen2.5-coder:7b", prompt, suffix, new CompletionProperties { Temperature = 0 });

			output.WriteLine(result.Content + "\n");
		}
		
		private async Task CompleteChat(int i, IEnumerable<IMessage> messages)
		{
			output.WriteLine($"[{i}] Completing chat...");

			var result = await client.CreateChatCompletionAsync(
				"qwen2.5-coder:7b", messages, new CompletionProperties { Temperature = 0 });

			output.WriteLine(result.Content + "\n");
		}

		[Fact]
		public async Task Completions()
		{
			await Complete(1, "Once upon a day, ", null);
			await Complete(2, "Write function in js: ```javascript\n", "```");
			await Complete(3, "Write function in python: ```python\ndef fibonacci", "```");
		}

		[Fact]
		public async Task ChatCompletions()
		{
			await CompleteChat(1, new UserMessage("Hello!").WrapIntoArray());
			await CompleteChat(2, new UserMessage("What is programming?").WrapIntoArray());
		}
	}
}