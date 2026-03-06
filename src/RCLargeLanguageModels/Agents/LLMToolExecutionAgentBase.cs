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
	public abstract class LLMToolExecutionAgentBase
	{
		/// <summary>
		/// Gets the maximum number of tool calls that can be received from language model before block it from tool use. If set to -1, there is no limit.
		/// </summary>
		protected virtual int MaxToolCalls => -1;

		/// <summary>
		/// Gets the maximum number of tool messages that can be received from language model before block it from tool use. If set to -1, there is no limit.
		/// </summary>
		protected virtual int MaxToolMessages => -1;

		/// <summary>
		/// Gets the previous messages that may contain memory data, e.g. a conversation history or just a system message with instructions.
		/// </summary>
		/// <param name="userMessage">The current user's message.</param>
		/// <returns>The previous messages with current user's message appended to the end.</returns>
		protected abstract IEnumerable<IMessage> GetMessages(IUserMessage userMessage);

		/// <summary>
		/// Gets the configured language model that will be used to generate responses.
		/// </summary>
		/// <returns>The language model that include tools, completion properties and other stuff.</returns>
		protected abstract LLModel GetLLM();

		/// <summary>
		/// Callback method that is invoked when a new message is received. This can be used to perform additional actions or logging.
		/// </summary>
		/// <param name="message">The new message that was received.</param>
		protected virtual void OnMessageReceived(IMessage message)
		{
		}

		/// <summary>
		/// Callback method that is invoked when a tool execution begins. This can be used to perform additional actions or logging.
		/// </summary>
		/// <param name="tool">The tool that is being executed.</param>
		/// <param name="toolCall">The tool call that is being executed.</param>
		/// <returns>The result of the tool execution to replace with actual tool result, or null if the tool should be executed normally.</returns>
		protected virtual Task<ToolResult?> OnToolExecutionBegin(ITool tool, IToolCall toolCall)
		{
			return Task.FromResult<ToolResult?>(null);
		}

		/// <summary>
		/// Callback method that is invoked when a tool execution ends. This can be used to perform additional actions or logging.
		/// </summary>
		/// <param name="tool">The tool that was executed.</param>
		/// <param name="toolCall">The tool call that was executed.</param>
		/// <param name="resultTask">The task that contains the result of the tool execution.</param>
		/// <returns>The result of the tool execution.</returns>
		protected virtual ToolResult OnToolExecutionEnd(ITool tool, IToolCall toolCall, Task<ToolResult> resultTask)
		{
			ToolResult toolMsgContent;

			if (resultTask.IsCanceled)
				toolMsgContent = "CANCELLED";
			else if (resultTask.IsFaulted)
				toolMsgContent = "ERROR";
			else
				toolMsgContent = resultTask.Result;

			return toolMsgContent;
		}

		/// <summary>
		/// Generates a response to the provided user message using the configured language model.
		/// </summary>
		/// <param name="userMessage">The current user's message.</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>The generated assistant messages mixed with tool messages as an asynchronous enumerable.</returns>
		public async Task<IEnumerable<IMessage>> GenerateResponseAsync(IUserMessage userMessage, CancellationToken cancellationToken = default)
		{
			var result = new List<IMessage>();

			var llm = GetLLM();
			var messages = GetMessages(userMessage).ToList();
			var toolset = llm.Tools;

			var response = await llm.ChatAsync(messages, cancellationToken);
			var message = response.Message;

			messages.Add(message);
			OnMessageReceived(message);
			result.Add(message);

			int toolCalls = 0, toolMessages = 0;

			while (true)
			{
				ConcurrentDictionary<IToolCall, IToolMessage> toolMessageMap = new();
				List<Task> toolExecutionTasks = new();
				object toolExecutionTasksLock = new();

				async Task ProcessToolCallAsync(IToolCall toolCall)
				{
					toolCalls++;

					switch (toolCall)
					{
						case FunctionToolCall functionCall:

							var tool = toolset.Get(toolCall.ToolName) as FunctionTool ??
								throw new InvalidOperationException($"FunctionTool '{functionCall.ToolName}' not found in the current toolset.");

							var toolResult = await OnToolExecutionBegin(tool, toolCall);

							if (toolResult != null)
							{
								toolResult = OnToolExecutionEnd(tool, toolCall, Task.FromResult(toolResult));
								toolMessageMap[toolCall] = new ToolMessage(toolResult, toolCall.Id, toolCall.ToolName);
							}
							else
							{
								await tool.ExecuteAsync(functionCall.Args, cancellationToken)
									.ContinueWith(t =>
									{
										ToolResult toolResult = OnToolExecutionEnd(tool, toolCall, t);
										toolMessageMap[toolCall] = new ToolMessage(toolResult, toolCall.Id, toolCall.ToolName);
									}, cancellationToken);
							}

							break;

						default:
							throw new InvalidOperationException($"Unknown tool call type: {toolCall.GetType()}.");
					}
				}

				foreach (var toolCall in message.ToolCalls)
				{
					toolExecutionTasks.Add(ProcessToolCallAsync(toolCall));
				}

				if (toolExecutionTasks.Count > 0)
				{
					toolMessages++;
				}
				if ((MaxToolCalls >= 0 && toolCalls >= MaxToolCalls) ||
					(MaxToolMessages >= 0 && toolMessages >= MaxToolMessages))
				{
					llm = llm.WithoutTools();
				}
				if (toolExecutionTasks.Count > 0)
				{
					await Task.WhenAll(toolExecutionTasks);

					// Send the tool results back to the LLM.

					foreach (var toolCall in message.ToolCalls)
					{
						var toolMessage = toolMessageMap[toolCall];
						messages.Add(toolMessage);
						OnMessageReceived(toolMessage);
						result.Add(toolMessage);
					}

					response = await llm.ChatAsync(messages, cancellationToken);
					message = response.Message;
					messages.Add(message);
					OnMessageReceived(message);
					result.Add(message);
				}
				else
				{
					break;
				}
			}

			return result;
		}

		/// <summary>
		/// Generates a response to the provided user message using the configured language model.
		/// </summary>
		/// <param name="userMessage">The current user's message.</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>The generated assistant messages mixed with tool messages as an asynchronous enumerable.</returns>
		public async IAsyncEnumerable<IMessage> GenerateStreamingResponseAsync(IUserMessage userMessage, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var llm = GetLLM();
			var messages = GetMessages(userMessage).ToList();
			var toolset = llm.Tools;

			var response = await llm.ChatStreamingAsync(messages, cancellationToken);
			var message = response.Message;

			messages.Add(message);
			OnMessageReceived(message);
			yield return message;

			int toolCalls = 0, toolMessages = 0;

			while (true)
			{
				ConcurrentDictionary<IToolCall, IToolMessage> toolMessageMap = new();
				List<Task> toolExecutionTasks = new();
				object toolExecutionTasksLock = new();

				async Task ProcessToolCallAsync(IToolCall toolCall)
				{
					toolCalls++;

					switch (toolCall)
					{
						case FunctionToolCall functionCall:

							var tool = toolset.Get(toolCall.ToolName) as FunctionTool ??
								throw new InvalidOperationException($"FunctionTool '{functionCall.ToolName}' not found in the current toolset.");

							var toolResult = await OnToolExecutionBegin(tool, toolCall);

							if (toolResult != null)
							{
								toolResult = OnToolExecutionEnd(tool, toolCall, Task.FromResult(toolResult));
								toolMessageMap[toolCall] = new ToolMessage(toolResult, toolCall.Id, toolCall.ToolName);
							}
							else
							{
								await tool.ExecuteAsync(functionCall.Args, cancellationToken)
									.ContinueWith(t =>
									{
										ToolResult toolResult = OnToolExecutionEnd(tool, toolCall, t);
										toolMessageMap[toolCall] = new ToolMessage(toolResult, toolCall.Id, toolCall.ToolName);
									}, cancellationToken);
							}

							break;

						default:
							throw new InvalidOperationException($"Unknown tool call type: {toolCall.GetType()}.");
					}
				}

				foreach (var toolCall in message.ToolCalls)
				{
					toolExecutionTasks.Add(ProcessToolCallAsync(toolCall));
				}

				void PartHandler(object? s, AssistantMessageDelta e)
				{
					if (e.NewToolCalls?.Count > 0)
						foreach (var toolCall in e.NewToolCalls)
							toolExecutionTasks.Add(ProcessToolCallAsync(toolCall));
				}
				void CompletedHandler(object? s, CompletedEventArgs e)
				{
					message.PartAdded -= PartHandler;
					message.Completed -= CompletedHandler;
				}

				try
				{
					message.PartAdded += PartHandler;
					message.Completed += CompletedHandler;
					await message;
				}
				finally
				{
					message.PartAdded -= PartHandler;
					message.Completed -= CompletedHandler;
				}

				if (toolExecutionTasks.Count > 0)
				{
					toolMessages++;
				}
				if ((MaxToolCalls >= 0 && toolCalls >= MaxToolCalls) ||
					(MaxToolMessages >= 0 && toolMessages >= MaxToolMessages))
				{
					llm = llm.WithoutTools();
				}
				if (toolExecutionTasks.Count > 0)
				{
					await Task.WhenAll(toolExecutionTasks);

					// Send the tool results back to the LLM.

					foreach (var toolCall in message.ToolCalls)
					{
						var toolMessage = toolMessageMap[toolCall];
						messages.Add(toolMessage);
						OnMessageReceived(toolMessage);
						yield return toolMessage;
					}

					response = await llm.ChatStreamingAsync(messages, cancellationToken);
					message = response.Message;
					messages.Add(message);
					OnMessageReceived(message);
					yield return message;
				}
				else
				{
					break;
				}
			}

			yield break;
		}
	}
}