using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RCLargeLanguageModels.Messages
{
	/// <summary>
	/// Reprents a branched message conversation history.
	/// </summary>
	public class BranchedMessageHistory : BranchedCollection<IMessage>
	{
		private readonly List<SystemMessage> _systemMessages = new List<SystemMessage>();

		/// <summary>
		/// Gets the read-only collection of system messages in the history.
		/// </summary>
		[JsonProperty]
		public IEnumerable<SystemMessage> SystemMessages { get; }

		/// <summary>
		/// Initializes a new empty branched message history.
		/// </summary>
		public BranchedMessageHistory()
		{
			SystemMessages = _systemMessages.AsReadOnly();
		}

		/// <summary>
		/// Initializes a new branched message history with the specified messages.
		/// </summary>
		/// <param name="messages">Initial messages to add (system messages will be filtered automatically).</param>
		public BranchedMessageHistory(IEnumerable<IMessage> messages)
		{
			SystemMessages = _systemMessages.AsReadOnly();
			AddRange(messages);
		}

		/// <summary>
		/// Initializes a new branched message history with the specified messages.
		/// </summary>
		/// <param name="messages">Initial messages to add (system messages will be filtered automatically).</param>
		public BranchedMessageHistory(params IMessage[] messages)
		{
			SystemMessages = _systemMessages.AsReadOnly();
			AddRange(messages);
		}

		[JsonConstructor]
		internal BranchedMessageHistory(IEnumerable<SystemMessage> systemMessages, BranchedEntry<IMessage> root)
			: base(ProcessRoot(root, ref systemMessages))
		{
			SystemMessages = _systemMessages.AsReadOnly();
			_systemMessages.AddRange(systemMessages);
		}

		private static BranchedEntry<IMessage> ProcessRoot(BranchedEntry<IMessage> root, ref IEnumerable<SystemMessage> systemMessages)
		{
			for (int i = 0; i < root.Items.Count; i++)
			{
				if (root.Items[i] is SystemMessage systemMessage)
				{
					systemMessages = systemMessages.Append(systemMessage);
					root.Items.RemoveAt(i);
					i--;
					continue;
				}

				if (root.Items[i] is PartialAssistantMessage partialAssistantMessage)
				{
					if (partialAssistantMessage.CompletionToken.State == Tasks.CompletionState.Incomplete)
						root.Items[i] = partialAssistantMessage.CopyCompleted();

					if (partialAssistantMessage.CompletionToken.State == Tasks.CompletionState.Success)
						root.Items[i] = partialAssistantMessage.AsAssistantMessage();
				}
			}

			foreach (var branch in root.Branches)
				ProcessRoot(branch, ref systemMessages);

			return root;
		}

		/// <summary>
		/// Adds a system message to the history.
		/// </summary>
		/// <param name="message">System message to add.</param>
		public void AddSystemMessage(SystemMessage message)
		{
			_systemMessages.Add(message);
		}

		/// <summary>
		/// Adds multiple system messages to the history.
		/// </summary>
		/// <param name="messages">System messages to add.</param>
		public void AddSystemMessages(IEnumerable<SystemMessage> messages)
		{
			_systemMessages.AddRange(messages);
		}

		/// <summary>
		/// Adds multiple system messages to the history.
		/// </summary>
		/// <param name="messages">System messages to add.</param>
		public void AddSystemMessages(params SystemMessage[] messages)
		{
			_systemMessages.AddRange(messages);
		}

		/// <summary>
		/// Replaces all system messages with a single new message.
		/// </summary>
		/// <param name="message">The new system message.</param>
		public void SetSystemMessage(SystemMessage message)
		{
			_systemMessages.Clear();
			_systemMessages.Add(message);
		}

		/// <summary>
		/// Replaces all system messages with the specified collection.
		/// </summary>
		/// <param name="messages">New system messages collection.</param>
		public void SetSystemMessages(IEnumerable<SystemMessage> messages)
		{
			_systemMessages.Clear();
			_systemMessages.AddRange(messages);
		}

		/// <summary>
		/// Replaces all system messages with the specified collection.
		/// </summary>
		/// <param name="messages">New system messages collection.</param>
		public void SetSystemMessages(params SystemMessage[] messages)
		{
			_systemMessages.Clear();
			_systemMessages.AddRange(messages);
		}

		/// <summary>
		/// Removes all system messages from the history.
		/// </summary>
		public void ClearSystemMessages()
		{
			_systemMessages.Clear();
		}

		private IMessage TryProcessPartialMessage(IMessage message)
		{
			if (message is PartialAssistantMessage partialAssistantMessage)
			{
				if (partialAssistantMessage.CompletionToken.State == Tasks.CompletionState.Success)
					return partialAssistantMessage.AsAssistantMessage();

				if (!partialAssistantMessage.CompletionToken.IsCompleted)
					partialAssistantMessage.CompletionToken.OnCompleted(() =>
					{
						try
						{
							if (partialAssistantMessage.CompletionToken.State == Tasks.CompletionState.Success)
								Replace(message, partialAssistantMessage.AsAssistantMessage());
						}
						catch { }
					});
			}

			return message;
		}

		/// <summary>
		/// Adds a message to the history (automatically handles system message routing).
		/// </summary>
		/// <param name="item">Message to add.</param>
		/// <exception cref="ArgumentNullException">Thrown if the message is null.</exception>
		public override void Add(IMessage item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			if (item is SystemMessage systemMessage)
				AddSystemMessage(systemMessage);
			else
				base.Add(TryProcessPartialMessage(item));
		}

		/// <summary>
		/// Adds multiple messages to the history (automatically routes system messages).
		/// </summary>
		/// <param name="items">Messages to add.</param>
		public override void AddRange(IEnumerable<IMessage> items)
		{
			var list = items.ToList();
			AddSystemMessages(list.OfType<SystemMessage>());
			base.AddRange(list.Where(m => !(m is SystemMessage)).Select(TryProcessPartialMessage));
		}

		/// <inheritdoc/>
		/// <exception cref="ArgumentException">Thrown when attempting to edit a message into a system message.</exception>
		public override void Edit(int sourceIndex, IMessage editedItem, BranchItemEditMode mode = BranchItemEditMode.Default)
		{
			if (editedItem is SystemMessage)
				throw new ArgumentException("Cannot edit to system message!", nameof(editedItem));

			base.Edit(sourceIndex, TryProcessPartialMessage(editedItem), mode);
		}

		/// <summary>
		/// Clears all messages (both system messages and regular messages) from the history.
		/// </summary>
		public override void Clear()
		{
			_systemMessages.Clear();
			base.Clear();
		}

		/// <summary>
		/// Gets the last messages from the history along with the system messages.
		/// </summary>
		/// <param name="maxCount">The maximum count of non-system messages to return.</param>
		/// <returns>The system messages with up to <paramref name="maxCount"/> non-system messages.</returns>
		public IEnumerable<IMessage> GetLastMessages(int maxCount)
		{
			if (maxCount < 0)
				throw new ArgumentOutOfRangeException(nameof(maxCount), "Count must be non-negative!");
			if (maxCount == 0)
				return SystemMessages;

			int countToReturn = Math.Min(maxCount, Items.Count);
			List<IMessage> result = new List<IMessage>(countToReturn + _systemMessages.Count);

			result.AddRange(SystemMessages);

			for (int i = Items.Count - countToReturn; i < Items.Count; i++)
				result.Add(Items[i].Item);

			return result;
		}

		/// <summary>
		/// Gets the last messages from the history along with the system messages.
		/// </summary>
		/// <param name="maxUserCount">The maximum count of user messages that will be present in collection.</param>
		/// <returns>The system messages with last messages that contains up to <paramref name="maxUserCount"/> user messages.</returns>
		/// <remarks>
		/// This method returns the collection that contains up to <paramref name="maxUserCount"/> user messages,
		/// but all messages before the first user message will be ignored, if maximum count was reached. <para/>
		/// For example: <para/>
		/// System: You are helpful assitant. <br/>
		/// User: Hello! <br/>
		/// Assistant: Hello! How can I help you? <br/>
		/// User: I want to know the weather. <br/>
		/// Assistant: The weather is sunny today. <br/>
		/// User: Thank you! <para/>
		/// The method with <paramref name="maxUserCount"/> = 2 will return: <para/>
		/// System: You are helpful assitant. <br/>
		/// User: I want to know the weather. <br/>
		/// Assistant: The weather is sunny today. <br/>
		/// User: Thank you!
		/// </remarks>
		public IEnumerable<IMessage> GetLastUserMessages(int maxUserCount)
		{
			if (maxUserCount < 0)
				throw new ArgumentOutOfRangeException(nameof(maxUserCount), "Count must be non-negative!");
			if (maxUserCount == 0 || Items.Count == 0)
				return SystemMessages;

			var result = new List<IMessage>();
			result.AddRange(_systemMessages);

			int c = 0;
			Stack<IMessage> stack = new Stack<IMessage>();

			for (int i = Items.Count - 1; i >= 0; i--)
			{
				stack.Push(Items[i].Item);
				if (Items[i].Item is UserMessage)
					c++;
				if (c == maxUserCount)
					break;
			}

			// The stack is filled with messages in reverse order, but stack is enumerated in the original order, so we dont need to reverse
			foreach (var item in stack)
			{
				result.Add(item);
			}

			return result;
		}
	}
}