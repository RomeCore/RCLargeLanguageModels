using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCLargeLanguageModels.Clients.Deepseek;
using RCLargeLanguageModels.Clients.Ollama;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Security;
using Xunit.Abstractions;

namespace RCLargeLanguageModels.Tests
{
	public class DeepSeekGenerationTests
	{
		private readonly LLMClient client;
		private readonly ITestOutputHelper output;

		public DeepSeekGenerationTests(ITestOutputHelper output)
		{
			this.client = new DeepSeekClient(new EnvironmentTokenAccessor("DEEPSEEK_API_KEY"));
			this.output = output;
		}

		private async Task Complete(int i, string prompt, string? suffix)
		{
			output.WriteLine($"[{i}] Completing...");

			var result = await client.CreateCompletionAsync(
				"deepseek-chat", prompt, suffix, new CompletionProperties { Temperature = 0 });

			output.WriteLine("Result:");
			output.WriteLine(prompt + result.Content + (suffix ?? "") + "\n");
		}
		
		private async Task CompleteAsync(int i, string prompt, string? suffix)
		{
			output.WriteLine($"[{i}] Completing...");

			var result = await client.CreateStreamingCompletionAsync(
				"deepseek-chat", prompt, suffix, new CompletionProperties { Temperature = 0 });

			await foreach (var delta in result.Completion)
			{
				output.WriteLine(delta.DeltaContent);
			}
			output.WriteLine("");
		}

		[Fact]
		public async Task Completions()
		{
			await Complete(1, "Once upon a day, ", "Happy ending!");
			await Complete(2, "```javascript\n", "```");
			await Complete(3, "def calc_fibonacci(n)", "return result");
		}

		[Fact]
		public async Task CompletionsAsync()
		{
			await CompleteAsync(1, "Once upon a day, ", "Happy ending!");
			await CompleteAsync(2, "Write function in js: ```javascript\n", "```");
			await CompleteAsync(3, "Write function in python: ```python\ndef fibonacci", "```");
		}
	}
}