using RCLargeLanguageModels;
using RCLargeLanguageModels.Agents;
using RCLargeLanguageModels.Clients.Deepseek;
using RCLargeLanguageModels.Clients.Ollama;
using RCLargeLanguageModels.Clients.OpenAI;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Embeddings;
using RCLargeLanguageModels.Embeddings.Database;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Security;
using RCLargeLanguageModels.Tools;

var openrouterClient = new OpenAICompatibleClient("https://openrouter.ai/api/v1", new EnvironmentTokenAccessor("OPENROUTER_API_KEY"));
var deepseekClient = new DeepSeekClient(new EnvironmentTokenAccessor("DEEPSEEK_API_KEY"));

var gpt = new LLModel(openrouterClient, "openai/gpt-5.4");
var minimax = new LLModel(openrouterClient, "minimax/minimax-m2.5");
var gemini = new LLModel(openrouterClient, "google/gemini-3-flash-preview");
var sonnet = new LLModel(openrouterClient, "anthropic/claude-sonnet-4.6");
var deepseek = new LLModel(deepseekClient, "deepseek-chat");

static string GetWeather(string city)
{
	return $"Weather in {city} is sunny.";
}
var weatherTool = FunctionTool.From(GetWeather, "get_weather");

var executor = new LLMToolExecutor
{
	LLMProvider = sonnet,
	Memory = new SummarizingChatMemory
	{
		SystemInstructions = "You are a helpful assistant.",
		TargetTokens = 128000,
		MaxSummarizerTokens = 128000,
		Summarizer = SummarizingChatMemory.CreateSummarizer(deepseek)
	}
};

executor.MessageReceived += (s, m) =>
{
	if (m is not IAssistantMessage)
		return;

	Console.Write(m.Content);
	if (m is PartialAssistantMessage pam)
	{
		if (!pam.CompletionToken.IsCompleted)
		{
			pam.PartAdded += (s, e) =>
			{
				Console.Write(e.DeltaContent);
			};
			pam.Completed += (s, e) =>
			{
				if (e.Exception != null)
					Console.WriteLine(e.Exception.Message);
				else
					Console.WriteLine();
			};
		}
		else
		{
			Console.WriteLine();
		}
	}
	else
	{
		Console.WriteLine();
	}
};

while (true)
{
	Console.Write("Enter your message: ");
	var input = Console.ReadLine() ?? string.Empty;

	if (!string.IsNullOrWhiteSpace(input))
	{
		await executor.GenerateStreamingResponseAsync(new UserMessage(input));
	}
}