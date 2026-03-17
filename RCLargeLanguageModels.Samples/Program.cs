using RCLargeLanguageModels;
using RCLargeLanguageModels.Agents;
using RCLargeLanguageModels.Clients.Deepseek;
using RCLargeLanguageModels.Clients.Ollama;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Embeddings;
using RCLargeLanguageModels.Embeddings.Database;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Security;
using RCLargeLanguageModels.Tools;


var deepseek = new DeepSeekClient(new EnvironmentTokenAccessor("DEEPSEEK_API_KEY"));
var deepseekModel = new LLModel(deepseek, "deepseek-chat");

var executor = new LLMToolExecutor
{
	LLMProvider = deepseekModel,
	Memory = new SummarizingChatMemory
	{
		SystemInstructions = "You are a helpful assistant.",
		TargetTokens = 16000,
		Summarizer = new StatelessAgent
		{
			SystemInstructions = "You are a conversation summarizer. " +
				"Your task is to summarize input LLM conversation history (including a previous summary, if provided) and return a next summary.",
			LLMProvider = deepseekModel
		}
	}
};

while (true)
{
	Console.Write("Enter your message: ");
	var input = Console.ReadLine() ?? string.Empty;

	if (!string.IsNullOrWhiteSpace(input))
	{
		var message = await executor.GenerateStreamingResponseAsync(new UserMessage(input));
		Console.WriteLine($"Received message: {message.Content}");
	}
}