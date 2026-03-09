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
	public abstract class LLMToolExecutorBase
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
		/// <param name="cancellationToken">The cancellation token that can be used to cancel the operation.</param>
		/// <returns>The result of the tool execution to replace with actual tool result, or null if the tool should be executed normally.</returns>
		protected virtual Task<ToolResult?> OnToolExecutionBegin(ITool tool, IToolCall toolCall, CancellationToken cancellationToken)
		{
			return Task.FromResult<ToolResult?>(null);
		}

		/// <summary>
		/// Callback method that is invoked when a tool execution ends. This can be used to perform additional actions or logging.
		/// </summary>
		/// <param name="tool">The tool that was executed.</param>
		/// <param name="toolCall">The tool call that was executed.</param>
		/// <param name="resultTask">The completed task that contains the result of the tool execution.</param>
		/// <returns>The result of the tool execution.</returns>
		protected virtual ToolResult OnToolExecutionEnd(ITool tool, IToolCall toolCall, Task<ToolResult> resultTask)
		{
			ToolResult toolMsgContent;

			if (resultTask.IsCanceled)
				toolMsgContent = new ToolResult(ToolResultStatus.Cancelled);
			else if (resultTask.IsFaulted)
				toolMsgContent = new ToolResult(ToolResultStatus.Error);
			else
				toolMsgContent = resultTask.Result;

			return toolMsgContent;
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
		public async Task<IEnumerable<IMessage>> GenerateResponseAsync(IUserMessage userMessage, CancellationToken cancellationToken = default)
		{
			var result = new List<IMessage>();

			var llm = GetLLM();
			var messages = GetMessages(userMessage).ToList();
			var toolset = llm.Tools;

			var response = await llm.ChatAsync(messages, cancellationToken: cancellationToken);
			var message = response.Message;

			messages.Add(message);
			OnMessageReceived(message);
			result.Add(message);

			int toolCallsCount = 0, toolMessagesCount = 0;

			while (true)
			{
				ConcurrentDictionary<IToolCall, IToolMessage> toolMessageMap = new();
				List<Task> toolExecutionTasks = new();

				foreach (var toolCall in message.ToolCalls)
				{
					toolCallsCount++;
					toolExecutionTasks.Add(ProcessToolCallAsync(toolCall, toolset, toolMessageMap, cancellationToken));
				}

				if (toolExecutionTasks.Count > 0)
				{
					toolMessagesCount++;
				}
				if ((MaxToolCalls >= 0 && toolCallsCount >= MaxToolCalls) ||
					(MaxToolMessages >= 0 && toolMessagesCount >= MaxToolMessages))
				{
					llm = llm.WithoutTools();
					toolset = ImmutableToolSet.Empty;
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

					response = await llm.ChatAsync(messages, cancellationToken: cancellationToken);
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

			var response = await llm.ChatStreamingAsync(messages, cancellationToken: cancellationToken);
			var message = response.Message;

			messages.Add(message);
			OnMessageReceived(message);
			yield return message;

			int toolCallsCount = 0, toolMessagesCount = 0;

			while (true)
			{
				ConcurrentDictionary<IToolCall, IToolMessage> toolMessageMap = new();
				List<Task> toolExecutionTasks = new();

				foreach (var toolCall in message.ToolCalls)
				{
					toolCallsCount++;
					toolExecutionTasks.Add(ProcessToolCallAsync(toolCall, toolset, toolMessageMap, cancellationToken));
				}

				void PartHandler(object? s, AssistantMessageDelta e)
				{
					if (e.NewToolCalls?.Count > 0)
						foreach (var toolCall in e.NewToolCalls)
						{
							toolCallsCount++;
							toolExecutionTasks.Add(ProcessToolCallAsync(toolCall, toolset, toolMessageMap, cancellationToken));
						}
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
					toolMessagesCount++;
				}
				if ((MaxToolCalls >= 0 && toolCallsCount >= MaxToolCalls) ||
					(MaxToolMessages >= 0 && toolMessagesCount >= MaxToolMessages))
				{
					llm = llm.WithoutTools();
					toolset = ImmutableToolSet.Empty;
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

					response = await llm.ChatStreamingAsync(messages, cancellationToken: cancellationToken);
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

		private async Task ProcessToolCallAsync(IToolCall toolCall, ImmutableToolSet toolset,
			ConcurrentDictionary<IToolCall, IToolMessage> toolMessageMap, CancellationToken cancellationToken)
		{
			switch (toolCall)
			{
				case FunctionToolCall functionCall:

					var tool = toolset.Get(toolCall.ToolName) as FunctionTool ??
						throw new InvalidOperationException($"FunctionTool '{functionCall.ToolName}' not found in the current toolset.");

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
						var toolResult = OnToolExecutionEnd(tool, toolCall, toolResultTask);
						toolMessageMap[toolCall] = new ToolMessage(toolResult, toolCall.Id, toolCall.ToolName);
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

						ToolResult toolResult = OnToolExecutionEnd(tool, toolCall, toolResultTask);

						if (string.IsNullOrEmpty(toolResult.Content) && toolResult.Attachments.Count == 0)
						{
							var newContent = GetToolFallbackContent(toolResult);
							toolResult = new ToolResult(toolResult.Status, newContent, toolResult.Attachments);
						}

						toolMessageMap[toolCall] = new ToolMessage(toolResult, toolCall.Id, toolCall.ToolName);
					}

					break;

				default:
					throw new InvalidOperationException($"Unknown tool call type: {toolCall.GetType()}.");
			}
		}
	}
}