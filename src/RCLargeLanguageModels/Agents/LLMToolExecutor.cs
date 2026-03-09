using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tools;

namespace RCLargeLanguageModels.Agents
{
	/// <summary>
	/// Represents a tool execution agent that simply gets responses from LLM and executes tool calls.
	/// This class provides a convenient implementation with configurable properties.
	/// </summary>
	public class LLMToolExecutor : LLMToolExecutorBase
	{
		private LLModel _llm;
		private readonly List<IMessage> _messages;
		private readonly Func<IUserMessage, IEnumerable<IMessage>>? _messageProvider;
		private int _maxToolCalls = -1;
		private int _maxToolMessages = -1;

		/// <summary>
		/// Initializes a new instance of the <see cref="LLMToolExecutor"/> class.
		/// </summary>
		public LLMToolExecutor()
		{
			_llm = LLModel.Empty;
			_messages = new List<IMessage>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LLMToolExecutor"/> class.
		/// </summary>
		/// <param name="llm">The language model to use for generating responses.</param>
		/// <param name="messages">Initial messages (e.g., system message, conversation history).</param>
		public LLMToolExecutor(LLModel llm, IEnumerable<IMessage>? messages = null)
		{
			_llm = llm ?? throw new ArgumentNullException(nameof(llm));
			_messages = messages?.ToList() ?? new List<IMessage>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LLMToolExecutor"/> class with a custom message provider.
		/// </summary>
		/// <param name="llm">The language model to use for generating responses.</param>
		/// <param name="messageProvider">Function that provides messages based on the user message.</param>
		public LLMToolExecutor(LLModel llm, Func<IUserMessage, IEnumerable<IMessage>> messageProvider)
		{
			_llm = llm ?? throw new ArgumentNullException(nameof(llm));
			_messageProvider = messageProvider ?? throw new ArgumentNullException(nameof(messageProvider));
			_messages = new List<IMessage>();
		}

		/// <summary>
		/// Gets or sets the maximum number of tool calls allowed (-1 for unlimited).
		/// </summary>
		public int MaxToolCallsCount
		{
			get => _maxToolCalls;
			set => _maxToolCalls = value;
		}

		/// <summary>
		/// Gets or sets the maximum number of messages that contains tool calls allowed (-1 for unlimited).
		/// </summary>
		public int MaxToolMessagesCount
		{
			get => _maxToolMessages;
			set => _maxToolMessages = value;
		}

		/// <summary>
		/// Gets the list of messages (conversation history, system messages, etc.).
		/// </summary>
		public List<IMessage> Messages => _messages;

		/// <summary>
		/// Gets the language model used by this agent.
		/// </summary>
		public LLModel LLM
		{
			get => _llm;
			set => _llm = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// Event triggered when a new message is received.
		/// </summary>
		public event EventHandler<IMessage>? MessageReceived;

		/// <summary>
		/// Event triggered when a tool execution begins.
		/// </summary>
		public event EventHandler<ToolExecutionBeginEventArgs>? ToolExecutionBegin;

		/// <summary>
		/// Event triggered when a tool execution ends.
		/// </summary>
		public event EventHandler<ToolExecutionEndEventArgs>? ToolExecutionEnd;

		/// <summary>
		/// Gets the maximum number of tool calls that can be received.
		/// </summary>
		protected override int MaxToolCalls => _maxToolCalls;

		/// <summary>
		/// Gets the maximum number of tool messages that can be received.
		/// </summary>
		protected override int MaxToolMessages => _maxToolMessages;

		/// <summary>
		/// Gets the configured language model.
		/// </summary>
		protected override LLModel GetLLM() => _llm;

		/// <summary>
		/// Gets the messages including the current user message.
		/// </summary>
		/// <param name="userMessage">The current user's message.</param>
		/// <returns>The messages with user message appended.</returns>
		protected override IEnumerable<IMessage> GetMessages(IUserMessage userMessage)
		{
			if (_messageProvider != null)
				return _messageProvider(userMessage);
			return new List<IMessage>(_messages) { userMessage };
		}

		/// <summary>
		/// Callback for when a message is received.
		/// </summary>
		protected override void OnMessageReceived(IMessage message)
		{
			base.OnMessageReceived(message);
			MessageReceived?.Invoke(this, message);
		}

		/// <summary>
		/// Callback for when a tool execution begins.
		/// </summary>
		protected override async Task<ToolResult?> OnToolExecutionBegin(ITool tool, IToolCall toolCall, CancellationToken cancellationToken)
		{
			var args = new ToolExecutionBeginEventArgs(tool, toolCall);
			ToolExecutionBegin?.Invoke(this, args);

			if (args.Result != null)
				return args.Result;

			return await base.OnToolExecutionBegin(tool, toolCall, cancellationToken);
		}

		/// <summary>
		/// Callback for when a tool execution ends.
		/// </summary>
		protected override ToolResult OnToolExecutionEnd(ITool tool, IToolCall toolCall, Task<ToolResult> resultTask)
		{
			var result = base.OnToolExecutionEnd(tool, toolCall, resultTask);
			var args = new ToolExecutionEndEventArgs(tool, toolCall, resultTask, result);
			ToolExecutionEnd?.Invoke(this, args);
			return args.Result ?? result;
		}

		/// <summary>
		/// Adds a message to the conversation history.
		/// </summary>
		/// <param name="message">The message to add.</param>
		public void AddMessage(IMessage message)
		{
			_messages.Add(message);
		}

		/// <summary>
		/// Adds multiple messages to the conversation history.
		/// </summary>
		/// <param name="messages">The messages to add.</param>
		public void AddMessages(IEnumerable<IMessage> messages)
		{
			_messages.AddRange(messages);
		}

		/// <summary>
		/// Clears all messages from the conversation history.
		/// </summary>
		public void ClearMessages()
		{
			_messages.Clear();
		}

		/// <summary>
		/// Creates a new agent with the same configuration but a clean message history.
		/// </summary>
		public LLMToolExecutor CreateCleanCopy()
		{
			return new LLMToolExecutor(_llm, new List<IMessage>())
			{
				MaxToolCallsCount = this.MaxToolCalls,
				MaxToolMessagesCount = this.MaxToolMessages
			};
		}

		/// <summary>
		/// Creates a new agent with the same configuration and a copy of the message history.
		/// </summary>
		public LLMToolExecutor CreateDeepCopy()
		{
			var messagesCopy = _messages.Select(m =>
			{
				if (m is PartialAssistantMessage pam)
					return pam.AsAssistantMessage();
				return m;
			}).ToList();

			return new LLMToolExecutor(_llm, messagesCopy)
			{
				MaxToolCallsCount = this.MaxToolCalls,
				MaxToolMessagesCount = this.MaxToolMessages
			};
		}
	}
}