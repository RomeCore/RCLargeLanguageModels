using System;
using Newtonsoft.Json.Linq;

namespace RCLargeLanguageModels
{
	/// <summary>
	/// Represents a large language model (LLM) and their's clients capabilities.
	/// </summary>
	[Flags]
	public enum LLMCapabilities
	{
		/// <summary>
		/// Capabilities are unknown. Use this flag if you sure of capabilities by yourself.
		/// </summary>
		Unknown = 0,

		// General capabilities

		/// <summary>
		/// Support of chat completions (generating 'assistant' message after input message sequence). <br/>
		/// General capability.
		/// </summary>
		ChatCompletions = 1 << 0,

		/// <summary>
		/// Support of general completions (generating completions using prompt). <br/>
		/// General capability.
		/// </summary>
		Completions = 1 << 1,

		/// <summary>
		/// Support of general fill-in-the-middle completions (generating completions using prompt and suffix). <br/>
		/// Implies <see cref="Completions"/> support. <br/>
		/// General capability.
		/// </summary>
		SuffixCompletions = Completions | 1 << 2,

		/// <summary>
		/// Support of embeddings (generating fixed-sized vectors from text). <br/>
		/// General capability.
		/// </summary>
		Embeddings = 1 << 3,

		/// <summary>
		/// Support of reranking (retrieving the relevant documents by query). <br/>
		/// General capability.
		/// </summary>
		Reranking = 1 << 4,

		// Generative capabilities

		/// <summary>
		/// Support of multiple chat and general completions per one request ('n' parameter). <br/>
		/// Completion capability.
		/// </summary>
		MultipleCompletions = 1 << 12,

		/// <summary>
		/// Support of streaming chat and general completions. <br/>
		/// Completion capability.
		/// </summary>
		StreamingCompletions = 1 << 13,

		/// <summary>
		/// Support of tool calling in chat completions. <br/>
		/// Chat completion capability.
		/// </summary>
		ToolSupport = 1 << 14,

		/// <summary>
		/// Support of generating COT (Chain-of-Thought) before generating main content. <br/>
		/// Completion capability.
		/// </summary>
		Reasoning = 1 << 15,

		/// <summary>
		/// Support of vision (reading raw image attachments). <br/>
		/// Completion capability.
		/// </summary>
		Vision = 1 << 16,
		
		/// <summary>
		/// Support of returning token probabilities (mostly known as logprobs) in the output completions. <br/>
		/// Completion capability.
		/// </summary>
		TokenProbabilities = 1 << 17,

		// Combined capabilities (ChatCompletions-based)

		/// <summary>
		/// Model supports chat completions with reasoning capabilities (Chain-of-Thought).
		/// Implies <see cref="ChatCompletions"/> and <see cref="Reasoning"/> support.
		/// </summary>
		ChatWithReasoning = ChatCompletions | Reasoning,

		/// <summary>
		/// Model supports chat completions with tool/function calling.
		/// Implies <see cref="ChatCompletions"/> and <see cref="ToolSupport"/> support.
		/// </summary>
		ChatWithTools = ChatCompletions | ToolSupport,

		/// <summary>
		/// Model supports chat completions with vision capabilities (multimodal).
		/// Implies <see cref="ChatCompletions"/> and <see cref="Vision"/> support.
		/// </summary>
		ChatWithVision = ChatCompletions | Vision,

		/// <summary>
		/// Model supports chat completions with both reasoning and tool calling.
		/// Implies <see cref="ChatCompletions"/>, <see cref="Reasoning"/> and <see cref="ToolSupport"/>.
		/// </summary>
		ChatWithReasoningAndTools = ChatCompletions | Reasoning | ToolSupport,

		/// <summary>
		/// Model supports chat completions with all advanced capabilities (reasoning, tools, and vision).
		/// Implies <see cref="ChatCompletions"/>, <see cref="Reasoning"/>, <see cref="ToolSupport"/>, and <see cref="Vision"/>.
		/// </summary>
		FullChatCapabilities = ChatCompletions | Reasoning | ToolSupport | Vision
	}

	/// <summary>
	/// Contains extension methods for the <see cref="LLMCapabilities"/> enum.
	/// </summary>
	public static class LLModelCapabilityExtension
	{
		public static void Validate(this LLMCapabilities capabilities)
		{
			if (capabilities == LLMCapabilities.Unknown)
				return;

			if (capabilities.HasGeneralCapability())
				return;

			throw new ArgumentException("Capabilities must contain at least ONE general capability.", nameof(capabilities));
		}
		
		public static LLMCapabilities CrossValidate(this LLMCapabilities capabilities)
		{
			capabilities.Validate();
			return capabilities;
		}
		
		public static LLMCapabilities? CrossValidate(this LLMCapabilities? capabilities)
		{
			capabilities?.Validate();
			return capabilities;
		}

		public static bool HasGeneralCapability(this LLMCapabilities capabilities)
		{
			return
				capabilities.HasFlag(LLMCapabilities.ChatCompletions) ||
				capabilities.HasFlag(LLMCapabilities.Completions) ||
				capabilities.HasFlag(LLMCapabilities.SuffixCompletions) ||
				capabilities.HasFlag(LLMCapabilities.Embeddings) ||
				capabilities.HasFlag(LLMCapabilities.Reranking);
		}

		public static bool IsUnknown(this LLMCapabilities capability) => capability == LLMCapabilities.Unknown;
		public static bool IsReasoning(this LLMCapabilities capability) => capability.HasFlag(LLMCapabilities.Reasoning);
		public static bool SupportsTools(this LLMCapabilities capability) => capability.HasFlag(LLMCapabilities.ToolSupport);
		public static bool SupportsVision(this LLMCapabilities capability) => capability.HasFlag(LLMCapabilities.Vision);
	}
}