# RCLargeLanguageModels

This C# library provides a clean, extensible API for working with large language models (LLMs). The main features are:
- Clean API to interact with LLM clients. Includes general/FIM/chat completions.
- The metadata-rich model classes that are useful for adaptive systems and UI applications, where users can select models based on their capabilities.
- The metadata collections that can be applied to clients, models, completion properties and completions themselves.

Note that library is in development stage and some features may not be implemented or be unstable (you can help the development, see the contribution guide in the last section).

## Installation

Since this library is not yet available on the NuGet, you can just clone the `main` branch and build it yourself or use precompiled DLLs that are available in the `Releases` section of this repository.  
NuGet will soon provide a package for easy installation.

## Usage

There is list of general classes that are used to interact with LLM:
- `LLMClient`: The base class of all LLM clients.
- `LLModelDescriptor`: The descriptor of the model, needed for interacting with API. It holds model name and includes optional information such as capabilities, context length, supported output formats, etc.
- `LLModel`: The convenient class that contains the parameters for LLM API. The parameters include the completion properties, tool list, output format, API queue parameters, etc. Functionality can be extended via completion property injectors.

### Examples

Creating a simple chat completion:

```csharp
using RCLargeLanguageModels;
using RCLargeLanguageModels.Clients.OpenAI;
using RCLargeLanguageModels.Messages;

// Configure the model
var client = new OpenAIClient("your-api-key");
var model = new LLModel(client, "gpt-3.5-turbo");

// Create the chat completion
var result = await model.ChatAsync([
	new SystemMessage("You are helpful assistant."),
	new UserMessage("What is the capital of France?")
]);

Console.WriteLine(result.Message.Content); // The capital of France is Paris.
// Or use the shortcut:
Console.WriteLine(result.Content);
```

Creating a streaming chat completion using environment token accessor, custom model descriptor and completion properties:

```csharp
using System.Text;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Clients.OpenAI;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Security;

// Configure the model
var token = new EnvironmentTokenAccessor("OPENAI_API_KEY");
var client = new OpenAIClient(token);
var descriptor = new LLModelDescriptor(client, "gpt-4-turbo", displayName: "GPT-4 Turbo");
var model = new LLModel(descriptor).WithProperties(
	new TemperatureProperty(0.7f),
	new MaxTokensProperty(1024));

// Create the chat completion
var result = await model.ChatStreamingAsync([
	new SystemMessage("You are helpful assistant. Respond just with answers without any additional text. Just the answer."),
	new UserMessage("What is the capital of Italy and France?")
]);

StringBuilder sb = new();

// Iterate through deltas
await foreach (var part in result.Message)
{
	Console.WriteLine("Got token: {0}", part.DeltaContent);
	// Or use implicit string conversion:
	sb.Append(part);
}

Console.WriteLine(sb.ToString()); // Rome and Paris.
// Or get the completed content:
Console.WriteLine(result.Content);
```

Or without `await foreach`, for C#<8.0:

```csharp
foreach (var _part in result.Message.GetTaskEnumerator())
{
	string part;
	// Since the _part is Task<> and it can throw exception on finish, we need to catch it
	try
	{
		part = await _part;
	}
	catch (TaskEnumerableFinishedException)
	{
		break;
	}

	Console.WriteLine("Got token: {0}", part);
}
```

Partial (streamng) messages can also be awaited:

```csharp
var result = await model.ChatStreamingAsync([
	...
]);

// Yes, it implements the Awaitable pattern
await result.Message;
Console.WriteLine(result.Content);
```

Using tools:

```csharp
// Declare the weather function in C#
string GetWeather(
	[Description("The location to get weather for")]
	string location
)
{
	// Just return the fixed data, for example
	return "24 C, Cloudy";
}

// Create the tool and give it to model
var tool = FunctionTool.From(GetWeather, "get_weather", "Gets weather for specified location");
model = model.WithToolAppend(tool);

// Get the completion
var result = await model.ChatAsync([
	new UserMessage("What's about weather in New York?")
]);

// Execute the called tool
var toolCall = result.Message.ToolCalls[0] as FunctionToolCall;
var callResult = await tool.ExecuteAsync(toolCall.Args);
Console.WriteLine(callResult.Content); // 24 C, Cloudy

// Create the tool message to put it in the message history
var message = new ToolMessage(callResult, toolCall.Id, toolCall.ToolName);
```

Creating multiple completions:

```csharp
var result = await model.ChatAsync([
	...
], count: 3);

foreach (var message in result.Choices)
{
	Console.WriteLine(message.Content);
}
```

The general completions:

```csharp
var result = await model.CompleteAsync(prompt: "Once upon a time, ",
	properties: new List<CompletionProperty> { new TemperatureProperty(0.5f) });
Console.WriteLine(result.Completion.Content);
// Or:
Console.WriteLine(result.Content);
```

The FIM completions:

```csharp
// Note that some models/clients are not supporting FIM completions
var result = await model.CompleteAsync(prompt: "```python\ndef fibonacci", suffix: "    return result```",
	properties: new List<CompletionProperty> { new TemperatureProperty(0.1f) });
Console.WriteLine(result.Content);
```

## Supported features and plans

### Supported clients:
- **Ollama**
- **DeepSeek**
- **Novita**
- **OpenAI** compatible clients
- *[Planned]* Full support for **OpenAI**
- *[Planned]* Support for **LlaMa.cpp** *(or LlaMaSharp)*

### LLM features:
- General completions
- FIM (fill-in-the-middle) completions
- Chat completions
- *[Planned]* Embeddings
- *[Planned]* Reranking

### General utilities:
- **Localization**: Language detection, grouping and fallback schemas
- **Task queues**: The priority queue of tasks to process in parallel or sequentially, useful for controlling local LLM calls. *Also planned rate limitters.*
- **Tool creation**: The easy creation of function tools from CLR methods or delegates.
- **Token storages**: The encrypted token storage with a simple interface to store and retrieve API tokens.
- **LLM client registry**: The registry of all available clients and their models with model availbility tracking, useful for UI applications.
- *[Planned]* **REST client**: The REST client that is optimized for debugging and SSE standards.
- *[Planned]* **Model metadata databases**: The model databases that stores and automatically retrieves model metadata, such as context length.
- *[Planned]* **Tokenizers**: The utilities to count tokens, truncate text/messages by token count, tokenize text, etc.

## Contributing

### Contributions are welcome!
If you have an idea about this project, you can report it to Issues.  
For contributing code, please fork the repository and make your changes in a new branch. Once you're ready, create a pull request to merge your changes into the main branch. Pull requests should include a clear description of what was changed and why.