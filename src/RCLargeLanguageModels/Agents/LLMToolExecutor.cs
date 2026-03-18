using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Messages.Attachments;
using RCLargeLanguageModels.Tasks;
using RCLargeLanguageModels.Tools;
using Serilog;

namespace RCLargeLanguageModels.Agents
{
	/// <summary>
	/// Represents a tool execution agent that simply gets responses from LLM and executes tool calls.
	/// </summary>
	public class LLMToolExecutor
	{
		/// <summary>
		/// The maximum number of tool calls that can be received from language model before block it from tool use. If set to negative, there is no limit.
		/// </summary>
		public int MaxToolCalls { get; set; } = -1;

		/// <summary>
		/// The maximum number of tool messages that can be received from language model before block it from tool use. If set to negative, there is no limit.
		/// </summary>
		public int MaxToolMessages { get; set; } = -1;

		/// <summary>
		/// The maximum number or parallel tool executions. If set to negative, there is no limit.
		/// </summary>
		public int MaxParallelToolExecutions { get; set; } = -1;

		/// <summary>
		/// The LLM provider that provides LLM for every conversation turn.
		/// </summary>
		public ILLMProvider? LLMProvider { get; set; } = null;

		/// <summary>
		/// The LLM chat memory that manages RAG, trimming, summarization and conversation transformations.
		/// </summary>
		public LLMChatMemory? Memory { get; set; } = null;

		/// <summary>
		/// Event that triggered when a new message is received in the processor, includes all messages (user, assistant and tool).
		/// </summary>
		public event EventHandler<IMessage>? MessageReceived;

		/// <summary>
		/// Event that triggered when a tool execution begins.
		/// </summary>
		public event EventHandler<ToolExecutionBeginEventArgs>? ToolExecutionBegin;

		/// <summary>
		/// Event that triggered when a tool execution ends.
		/// </summary>
		public event EventHandler<ToolExecutionEndEventArgs>? ToolExecutionEnd;

		/// <summary>
		/// Callback mehod that is invoked when specific message is added to the memory/context.
		/// </summary>
		/// <param name="message">The message that added to the memory.</param>
		protected virtual void OnMessageReceived(IMessage message)
		{
			MessageReceived?.Invoke(this, message);
		}

		/// <summary>
		/// Callback method that is invoked when a tool execution begins. This can be used to perform additional actions or logging.
		/// </summary>
		/// <param name="tool">The tool that is being executed.</param>
		/// <param name="toolCall">The tool call that is being executed.</param>
		/// <param name="cancellationToken">The cancellation token that can be used to cancel the operation.</param>
		/// <returns>The result of the tool execution to replace with actual tool result, or null if the tool should be executed normally.</returns>
		protected virtual Task<ToolResult?> OnToolExecutionBegin(ITool tool, IToolCall toolCall, CancellationToken cancellationToken)
		{
			var args = new ToolExecutionBeginEventArgs(tool, toolCall);
			ToolExecutionBegin?.Invoke(this, args);
			return Task.FromResult<ToolResult?>(args.Result);
		}

		/// <summary>
		/// Callback method that is invoked when a tool execution ends. This can be used to perform additional actions or logging.
		/// </summary>
		/// <param name="tool">The tool that was executed.</param>
		/// <param name="toolCall">The tool call that was executed.</param>
		/// <param name="resultTask">The completed task that contains the result of the tool execution.</param>
		/// <param name="cancellationToken">The cancellation token that can be used to cancel the operation.</param>
		/// <returns>The result of the tool execution.</returns>
		protected virtual Task<ToolResult> OnToolExecutionEnd(ITool tool, IToolCall toolCall, Task<ToolResult> resultTask, CancellationToken cancellationToken)
		{
			ToolResult toolMsgContent;

			if (resultTask.IsCanceled)
				toolMsgContent = new ToolResult(ToolResultStatus.Cancelled);
			else if (resultTask.IsFaulted)
				toolMsgContent = new ToolResult(ToolResultStatus.Error);
			else
				toolMsgContent = resultTask.Result;

			var args = new ToolExecutionEndEventArgs(tool, toolCall, resultTask, toolMsgContent);
			ToolExecutionEnd?.Invoke(this, args);
			return Task.FromResult(args.Result ?? toolMsgContent);
		}

		/// <summary>
		/// Gets the fallback LLM-readable content for a tool result if no specific content is provided.
		/// </summary>
		/// <param name="result">The tool result that does not contain specific content (text or attachments).</param>
		/// <returns>The fallback content for the tool result.</returns>
		protected virtual string GetToolFallbackContent(ToolResult result)
		{
			return result.Status switch
			{
				ToolResultStatus.Cancelled => "Tool execution was cancelled.",
				ToolResultStatus.Error => "Tool execution resulted in an error.",
				ToolResultStatus.Success => "Tool execution was successful.",
				ToolResultStatus.NoResult => "Tool did not produce any result.",
				_ => "Unknown tool execution status."
			};
		}

		/// <summary>
		/// Generates a response to the provided user message using the configured language model.
		/// </summary>
		/// <param name="userMessage">The current user's message.</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>The generated assistant messages mixed with tool messages as an asynchronous enumerable.</returns>
		public async Task<IAssistantMessage> GenerateResponseAsync(IUserMessage userMessage, CancellationToken cancellationToken = default)
		{
			var provider = LLMProvider;
			var memory = Memory;
			var maxToolCalls = MaxToolCalls;
			var maxToolMessages = MaxToolMessages;
			var maxParallelToolExecutions = MaxParallelToolExecutions;

			if (provider == null)
				throw new InvalidOperationException($"LLM provider is not set. See property {nameof(LLMProvider)}");
			if (memory == null)
				throw new InvalidOperationException($"Memory is not set. See property {nameof(Memory)}");

			OnMessageReceived(userMessage);
			var llm = provider.GetLLM();
			var messages = (await memory.AppendAsync(userMessage, llm, cancellationToken)).ToList();
			var toolset = llm.Tools;

			var response = await llm.ChatAsync(messages, cancellationToken: cancellationToken);
			var responseMessage = response.Message;
			OnMessageReceived(responseMessage);

			int toolCallsCount = 0, toolMessagesCount = 0;

			while (true)
			{
				ConcurrentDictionary<IToolCall, IToolMessage> toolMessageMap = new();
				List<Task> toolExecutionTasks = new();
				SemaphoreSlim? semaphore = maxParallelToolExecutions > 0 ?
					new SemaphoreSlim(maxParallelToolExecutions, maxParallelToolExecutions) :
					null;

				foreach (var toolCall in responseMessage.ToolCalls)
				{
					toolCallsCount++;
					toolExecutionTasks.Add(ProcessToolCallAsync(toolCall, toolset, semaphore, toolMessageMap, cancellationToken));
				}

				if (toolExecutionTasks.Count > 0)
				{
					toolMessagesCount++;
				}
				if ((maxToolCalls >= 0 && toolCallsCount >= maxToolCalls) ||
					(maxToolMessages >= 0 && toolMessagesCount >= maxToolMessages))
				{
					llm = llm.WithoutTools();
					toolset = ImmutableToolSet.Empty;
				}
				if (toolExecutionTasks.Count > 0)
				{
					await Task.WhenAll(toolExecutionTasks);
					var toolMessages = new List<IToolMessage>();

					foreach (var toolCall in responseMessage.ToolCalls)
					{
						var toolMessage = toolMessageMap[toolCall];
						toolMessages.Add(toolMessage);
					}

					messages = (await memory.AppendAsync(messages, responseMessage, toolMessages, llm, cancellationToken)).ToList();
					response = await llm.ChatAsync(messages, cancellationToken: cancellationToken);
					responseMessage = response.Message;
					OnMessageReceived(responseMessage);
				}
				else
				{
					break;
				}
			}

			var finalMessage = (await memory.AppendAsync(messages, responseMessage, Array.Empty<IToolMessage>(), llm, cancellationToken)).LastOrDefault();
			if (finalMessage is not IAssistantMessage finalAssistantMessage)
				throw new InvalidCastException($"Memory returned a sequence of messages that not finishes with assistant message. Got message: {finalMessage?.GetType()}.");
			return finalAssistantMessage;
		}

		/// <summary>
		/// Generates a response to the provided user message using the configured language model.
		/// </summary>
		/// <param name="userMessage">The current user's message.</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>The generated assistant messages mixed with tool messages as an asynchronous enumerable.</returns>
		public async Task<IAssistantMessage> GenerateStreamingResponseAsync(IUserMessage userMessage, CancellationToken cancellationToken = default)
		{
			var provider = LLMProvider;
			var memory = Memory;
			var maxToolCalls = MaxToolCalls;
			var maxToolMessages = MaxToolMessages;
			var maxParallelToolExecutions = MaxParallelToolExecutions;

			if (provider == null)
				throw new InvalidOperationException($"LLM provider is not set. See property {nameof(LLMProvider)}");
			if (memory == null)
				throw new InvalidOperationException($"Memory is not set. See property {nameof(Memory)}");

			OnMessageReceived(userMessage);
			var llm = provider.GetLLM();
			var messages = (await memory.AppendAsync(userMessage, llm, cancellationToken)).ToList();
			var toolset = llm.Tools;

			var response = await llm.ChatStreamingAsync(messages, cancellationToken: cancellationToken);
			var responseMessage = response.Message;
			OnMessageReceived(responseMessage);

			int toolCallsCount = 0, toolMessagesCount = 0;

			while (true)
			{
				ConcurrentDictionary<IToolCall, IToolMessage> toolMessageMap = new();
				List<Task> toolExecutionTasks = new();
				SemaphoreSlim? semaphore = maxParallelToolExecutions > 0 ?
					new SemaphoreSlim(maxParallelToolExecutions, maxParallelToolExecutions) :
					null;

				foreach (var toolCall in responseMessage.ToolCalls)
				{
					toolCallsCount++;
					toolExecutionTasks.Add(ProcessToolCallAsync(toolCall, toolset, semaphore, toolMessageMap, cancellationToken));
				}

				void PartHandler(object? s, AssistantMessageDelta e)
				{
					if (e.NewToolCalls?.Count > 0)
						foreach (var toolCall in e.NewToolCalls)
						{
							toolCallsCount++;
							toolExecutionTasks.Add(ProcessToolCallAsync(toolCall, toolset, semaphore, toolMessageMap, cancellationToken));
						}
				}
				void CompletedHandler(object? s, CompletedEventArgs e)
				{
					responseMessage.PartAdded -= PartHandler;
					responseMessage.Completed -= CompletedHandler;
				}

				try
				{
					responseMessage.PartAdded += PartHandler;
					responseMessage.Completed += CompletedHandler;
					await responseMessage;
				}
				finally
				{
					responseMessage.PartAdded -= PartHandler;
					responseMessage.Completed -= CompletedHandler;
				}

				if (toolExecutionTasks.Count > 0)
				{
					toolMessagesCount++;
				}
				if ((maxToolCalls >= 0 && toolCallsCount >= maxToolCalls) ||
					(maxToolMessages >= 0 && toolMessagesCount >= maxToolMessages))
				{
					llm = llm.WithoutTools();
					toolset = ImmutableToolSet.Empty;
				}
				if (toolExecutionTasks.Count > 0)
				{
					await Task.WhenAll(toolExecutionTasks);
					var toolMessages = new List<IToolMessage>();

					foreach (var toolCall in responseMessage.ToolCalls)
					{
						var toolMessage = toolMessageMap[toolCall];
						toolMessages.Add(toolMessage);
					}

					messages = (await memory.AppendAsync(messages, responseMessage, toolMessages, llm, cancellationToken)).ToList();
					response = await llm.ChatStreamingAsync(messages, cancellationToken: cancellationToken);
					responseMessage = response.Message;
					OnMessageReceived(responseMessage);
				}
				else
				{
					break;
				}
			}

			var finalMessage = (await memory.AppendAsync(messages, responseMessage, Array.Empty<IToolMessage>(), llm, cancellationToken)).LastOrDefault();
			if (finalMessage is not IAssistantMessage finalAssistantMessage)
				throw new InvalidCastException($"Memory returned a sequence of messages that not finishes with assistant message. Got message: {finalMessage?.GetType()}.");
			return finalAssistantMessage;
		}

		private async Task ProcessToolCallAsync(IToolCall toolCall, ImmutableToolSet toolset,
			SemaphoreSlim? semaphore, ConcurrentDictionary<IToolCall, IToolMessage> toolMessageMap,
			CancellationToken cancellationToken)
		{
			switch (toolCall)
			{
				case FunctionToolCall functionCall:

					var tool = toolset.Get(toolCall.ToolName) as FunctionTool ??
						throw new InvalidOperationException($"FunctionTool '{functionCall.ToolName}' not found in the current toolset.");

					try
					{
						if (semaphore != null)
							await semaphore.WaitAsync(cancellationToken);
						Task<ToolResult?> toolResultTask;

						try
						{
							toolResultTask = OnToolExecutionBegin(tool, toolCall, cancellationToken);
							await toolResultTask;
						}
						catch (Exception ex)
						{
							toolResultTask = Task.FromException<ToolResult>(ex);
						}

						if (toolResultTask != null && toolResultTask.IsCompletedSuccessfully() && toolResultTask.Result != null)
						{
							var toolResult = await OnToolExecutionEnd(tool, toolCall, toolResultTask, cancellationToken);
							var toolMessage = new ToolMessage(toolResult, toolCall.Id, toolCall.ToolName);
							OnMessageReceived(toolMessage);
							toolMessageMap[toolCall] = toolMessage;
						}
						else
						{
							try
							{
								toolResultTask = tool.ExecuteAsync(functionCall.Args, cancellationToken);
								await toolResultTask;
							}
							catch (Exception ex)
							{
								toolResultTask = Task.FromException<ToolResult>(ex);
							}

							ToolResult toolResult = await OnToolExecutionEnd(tool, toolCall, toolResultTask, cancellationToken);

							if (string.IsNullOrEmpty(toolResult.Content) && toolResult.Attachments.Count == 0)
							{
								var newContent = GetToolFallbackContent(toolResult);
								toolResult = new ToolResult(toolResult.Status, newContent, toolResult.Attachments);
							}

							var toolMessage = new ToolMessage(toolResult, toolCall.Id, toolCall.ToolName);
							OnMessageReceived(toolMessage);
							toolMessageMap[toolCall] = toolMessage;
						}
					}
					finally
					{
						semaphore?.Release();
					}


					break;

				default:
					throw new InvalidOperationException($"Unknown tool call type: {toolCall.GetType()}.");
			}
		}
	}
}