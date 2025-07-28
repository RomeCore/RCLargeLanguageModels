using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RCLargeLanguageModels.Messages
{
	// Как же я люблю генерировать комментарии дипсиком (˶ᵔᵕᵔ˶)

	/// <summary>
	/// Represents a thread-safe collection of chat messages, including both system-generated and user messages.
	/// </summary> 
	[JsonArray(ItemTypeNameHandling = TypeNameHandling.Auto)]
	public class MessageHistory : IEnumerable<IMessage>, INotifyCollectionChanged
	{
		private readonly List<IMessage> _messages;
		private readonly List<SystemMessage> _systemMessages;
		private readonly object syncLock = new object();

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		/// <summary>
		/// Gets the collection of non-system messages in chronological order.
		/// </summary>
		/// <remarks>
		/// This collection is initialized as an empty list and should be populated with user messages. <br/>
		/// The list is maintained in chronological order (oldest to newest). <br/>
		/// This property is thread-safe for reading but reflects the state at the time of access.
		/// </remarks>
		public ReadOnlyCollection<IMessage> Messages { get; }

		/// <summary>
		/// Gets the collection of system-generated messages.
		/// </summary>
		/// <remarks>
		/// System messages typically include notifications, status updates, or bot responses. <br/>
		/// This property is thread-safe for reading but reflects the state at the time of access.
		/// </remarks>
		public ReadOnlyCollection<SystemMessage> SystemMessages { get; }

		/// <summary>
		/// Gets a value indicating whether the non-system message history is empty.
		/// </summary>
		/// <remarks>
		/// System messages are not counted, as LLMs typically require user input to generate content. <br/>
		/// This property is thread-safe and reflects the current state of non-system messages.
		/// </remarks>
		public bool IsEmpty
		{
			get
			{
				lock (syncLock)
				{
					return _messages.Count == 0;
				}
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MessageHistory"/> class with an empty message history.
		/// </summary>
		public MessageHistory()
		{
			_messages = new List<IMessage>();
			_systemMessages = new List<SystemMessage>();

			Messages = new ReadOnlyCollection<IMessage>(_messages);
			SystemMessages = new ReadOnlyCollection<SystemMessage>(_systemMessages);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MessageHistory"/> class by copying an existing history.
		/// </summary>
		/// <param name="copyFrom">The message history to copy from.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="copyFrom"/> is null.</exception>
		public MessageHistory(MessageHistory copyFrom)
		{
			if (copyFrom == null)
				throw new ArgumentNullException(nameof(copyFrom));

			lock (copyFrom.syncLock)
			{
				_messages = new List<IMessage>(copyFrom._messages);
				_systemMessages = new List<SystemMessage>(copyFrom._systemMessages);
			}

			Messages = new ReadOnlyCollection<IMessage>(_messages);
			SystemMessages = new ReadOnlyCollection<SystemMessage>(_systemMessages);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MessageHistory"/> class with the specified messages.
		/// </summary>
		/// <param name="messages">The messages to add; system messages are added to <see cref="SystemMessages"/>,
		/// others to <see cref="Messages"/>; empty messages are ignored.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="messages"/> is null.</exception>
		public MessageHistory(IEnumerable<IMessage> messages) : this()
		{
			if (messages == null)
				throw new ArgumentNullException(nameof(messages));

			AddRange(messages);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MessageHistory"/> class with the specified messages.
		/// </summary>
		/// <param name="messages">The messages to add; system messages are added to <see cref="SystemMessages"/>,
		/// others to <see cref="Messages"/>; empty messages are ignored.</param>
		public MessageHistory(params IMessage[] messages) : this(messages as IEnumerable<IMessage>)
		{
		}

		/// <summary>
		/// Adds a system message to the history.
		/// </summary>
		/// <param name="message">System message to add.</param>
		public void AddSystemMessage(SystemMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));
			_systemMessages.Add(message);
		}
		
		/// <summary>
		/// Adds a system message to the history.
		/// </summary>
		/// <param name="message">System message to add.</param>
		public void AddSystemMessage(string message)
		{
			if (string.IsNullOrEmpty(message))
				throw new ArgumentNullException(nameof(message));
			_systemMessages.Add(new SystemMessage(message));
		}

		/// <summary>
		/// Adds multiple system messages to the history.
		/// </summary>
		/// <param name="messages">System messages to add.</param>
		public void AddSystemMessages(IEnumerable<SystemMessage> messages)
		{
			if (messages == null)
				throw new ArgumentNullException(nameof(messages));
			_systemMessages.AddRange(messages);
		}

		/// <summary>
		/// Adds a system message to the history.
		/// </summary>
		/// <param name="messages">System messages to add.</param>
		public void AddSystemMessages(IEnumerable<string> messages)
		{
			if (messages == null)
				throw new ArgumentNullException(nameof(messages));
			_systemMessages.AddRange(messages.Select(m =>
			{
				if (string.IsNullOrEmpty(m))
					throw new ArgumentNullException(nameof(messages), "Elements of the collection cannot be null or empty.");
				return new SystemMessage(m);
			}));
		}

		/// <summary>
		/// Adds multiple system messages to the history.
		/// </summary>
		/// <param name="messages">System messages to add.</param>
		public void AddSystemMessages(params SystemMessage[] messages)
		{
			if (messages == null)
				throw new ArgumentNullException(nameof(messages));
			_systemMessages.AddRange(messages.Select(m =>
			{
				if (m == null)
					throw new ArgumentNullException(nameof(messages), "Elements of the collection cannot be null or whitespace.");
				return m;
			}));
		}

		/// <summary>
		/// Replaces all system messages with a single new message.
		/// </summary>
		/// <param name="message">The new system message.</param>
		public void SetSystemMessage(SystemMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));
			_systemMessages.Clear();
			_systemMessages.Add(message);
		}
		
		/// <summary>
		/// Replaces all system messages with a single new message.
		/// </summary>
		/// <param name="message">The new system message.</param>
		public void SetSystemMessage(string message)
		{
			if (string.IsNullOrEmpty(message))
				throw new ArgumentNullException(nameof(message));
			_systemMessages.Clear();
			_systemMessages.Add(new SystemMessage(message));
		}

		/// <summary>
		/// Replaces all system messages with the specified collection.
		/// </summary>
		/// <param name="messages">New system messages collection.</param>
		public void SetSystemMessages(IEnumerable<SystemMessage> messages)
		{
			if (messages == null)
				throw new ArgumentNullException(nameof(messages));
			var list = messages.Select(m =>
			{
				if (m == null)
					throw new ArgumentNullException(nameof(messages), "Elements of the collection cannot be null.");
				return m;
			});
			_systemMessages.Clear();
			_systemMessages.AddRange(list);
		}
		
		/// <summary>
		/// Replaces all system messages with the specified collection.
		/// </summary>
		/// <param name="messages">New system messages collection.</param>
		public void SetSystemMessages(IEnumerable<string> messages)
		{
			if (messages == null)
				throw new ArgumentNullException(nameof(messages));
			var list = messages.Select(m =>
			{
				if (string.IsNullOrEmpty(m))
					throw new ArgumentNullException(nameof(messages), "Elements of the collection cannot be null or empty.");
				return new SystemMessage(m);
			});
			_systemMessages.Clear();
			_systemMessages.AddRange(list);
		}

		/// <summary>
		/// Replaces all system messages with the specified collection.
		/// </summary>
		/// <param name="messages">New system messages collection.</param>
		public void SetSystemMessages(params SystemMessage[] messages)
		{
			SetSystemMessages(messages as IEnumerable<SystemMessage>);
		}
		
		/// <summary>
		/// Replaces all system messages with the specified collection.
		/// </summary>
		/// <param name="messages">New system messages collection.</param>
		public void SetSystemMessages(params string[] messages)
		{
			SetSystemMessages(messages as IEnumerable<string>);
		}

		/// <summary>
		/// Removes all system messages from the history.
		/// </summary>
		public void ClearSystemMessages()
		{
			_systemMessages.Clear();
		}

		/// <summary>
		/// Retrieves a combined sequence of system messages and the most recent non-system messages.
		/// </summary>
		/// <returns>
		/// A sequence containing all system messages and most recent non-system messages.
		/// </returns>
		/// <remarks>
		/// This method is thread-safe and returns a snapshot of the messages at the time of invocation.
		/// </remarks>
		public IEnumerable<IMessage> GetMessages()
		{
			lock (syncLock)
			{
				return SystemMessages.Concat(Messages).ToList();
			}
		}

		/// <summary>
		/// Retrieves a combined sequence of system messages and the most recent non-system messages, then adds a single message.
		/// </summary>
		/// <param name="message">The message to add after retrieving the current history.</param>
		/// <param name="maxNonSystemCount">Maximum number of non-system messages to include from the end of the Messages list, or -1 for all.</param>
		/// <returns>
		/// A sequence containing all system messages followed by up to <paramref name="maxNonSystemCount"/> most recent non-system messages,
		/// representing the state before the new message is added.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
		/// <remarks>
		/// This method is thread-safe and atomic. The returned sequence does not include the newly added message.
		/// Empty messages are ignored during the add operation.
		/// </remarks>
		public IEnumerable<IMessage> GetAndAddMessage(IMessage message, int maxNonSystemCount = -1)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			lock (syncLock)
			{
				// Retrieve the current state
				var result = maxNonSystemCount < 0 || Messages.Count <= maxNonSystemCount
					? SystemMessages.Concat(Messages).ToList()
					: SystemMessages.Concat(Messages.Skip(Messages.Count - maxNonSystemCount)).ToList();

				if (message is SystemMessage systemMessage)
					_systemMessages.Add(systemMessage);
				else
					_messages.Add(message);

				return result;
			}
		}

		/// <summary>
		/// Retrieves a combined sequence of system messages and the most recent non-system messages, then adds a collection of messages.
		/// </summary>
		/// <param name="messagesToAdd">The messages to add after retrieving the current history.</param>
		/// <param name="maxNonSystemCount">Maximum number of non-system messages to include from the end of the Messages list, or -1 for all.</param>
		/// <returns>
		/// A sequence containing all system messages followed by up to <paramref name="maxNonSystemCount"/> most recent non-system messages,
		/// representing the state before the new messages are added.
		/// </returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="messagesToAdd"/> is null.</exception>
		/// <remarks>
		/// This method is thread-safe and atomic. The returned sequence does not include the newly added messages.
		/// Empty messages are ignored during the add operation.
		/// </remarks>
		public IEnumerable<IMessage> GetAndAddMessages(IEnumerable<IMessage> messagesToAdd, int maxNonSystemCount = -1)
		{
			if (messagesToAdd == null)
				throw new ArgumentNullException(nameof(messagesToAdd));

			lock (syncLock)
			{
				// Retrieve the current state
				var result = maxNonSystemCount < 0 || Messages.Count <= maxNonSystemCount
					? SystemMessages.Concat(Messages).ToList()
					: SystemMessages.Concat(Messages.Skip(Messages.Count - maxNonSystemCount)).ToList();

				// Add the new messages
				foreach (var message in messagesToAdd)
				{
					if (message == null)
						throw new ArgumentNullException(nameof(message), "Message cannot be null.");

					if (message is SystemMessage systemMessage)
						_systemMessages.Add(systemMessage);
					else
						_messages.Add(message);
				}

				return result;
			}
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

			int countToReturn = Math.Min(maxCount, _messages.Count);
			List<IMessage> result = new List<IMessage>(countToReturn + _systemMessages.Count);

			result.AddRange(SystemMessages);

			for (int i = _messages.Count - countToReturn; i < _messages.Count; i++)
				result.Add(_messages[i]);

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
			if (maxUserCount == 0 || _messages.Count == 0)
				return SystemMessages;

			var result = new List<IMessage>();
			result.AddRange(_systemMessages);

			int c = 0;
			Stack<IMessage> stack = new Stack<IMessage>();

			for (int i = _messages.Count - 1; i >= 0; i--)
			{
				stack.Push(_messages[i]);
				if (_messages[i] is UserMessage)
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

		/// <summary>
		/// Appends a new message to the message history.
		/// </summary>
		/// <remarks>
		/// System messages are added to <see cref="SystemMessages"/>, others to <see cref="Messages"/>.
		/// This operation is thread-safe.
		/// </remarks>
		/// <param name="message">The message to append.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
		public void Add(IMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			lock (syncLock)
			{
				if (message is SystemMessage systemMessage)
					_systemMessages.Add(systemMessage);
				else
				{
					var index = _messages.Count;
					_messages.Add(message);
					CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, message, index));
				}
			}
		}

		/// <summary>
		/// Appends a new message to the message history.
		/// </summary>
		/// <remarks>
		/// System messages are added to <see cref="SystemMessages"/>, others to <see cref="Messages"/>.
		/// This operation is thread-safe.
		/// </remarks>
		/// <param name="role">The message role to append.</param>
		/// <param name="content">The message content to append.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="role"/> is out of range.</exception>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="content"/> is null.</exception>
		public void Add(Role role, string content)
		{
			if (!Enum.IsDefined(typeof(Role), role))
				throw new ArgumentOutOfRangeException("Invalid role.", nameof(role));
			if (content == null)
				throw new ArgumentNullException(nameof(content));

			lock (syncLock)
			{
				if (role == Role.System)
					_systemMessages.Add(new SystemMessage(content));
				else
				{
					var index = _messages.Count;
					IMessage message;
					switch (role)
					{
						case Role.User:
							message = new UserMessage(content);
							break;
						case Role.Assistant:
							message = new AssistantMessage(content, null as string);
							break;
						case Role.Tool:
							message = new ToolMessage(content, string.Empty, string.Empty);
							break;
						default:
							throw new ArgumentException("Invalid role.", nameof(role));
					}

					_messages.Add(message);
					CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, message, index));
				}
			}
		}

		/// <summary>
		/// Appends a collection of messages to the message history.
		/// </summary>
		/// <remarks>
		/// System messages are added to <see cref="SystemMessages"/>, others to <see cref="Messages"/>.
		/// </remarks>
		/// <param name="messages">The messages to append.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="messages"/> is null.</exception>
		public void AddRange(IEnumerable<IMessage> messages)
		{
			if (messages == null)
				throw new ArgumentNullException(nameof(messages));

			lock (syncLock)
			{
				foreach (var message in messages)
				{
					if (message != null)
					{
						if (message is SystemMessage systemMessage)
							_systemMessages.Add(systemMessage);
						else
						{
							var index = _messages.Count;
							_messages.Add(message);
							CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, message, index));
						}
					}
				}
			}
		}

		/// <summary>
		/// Clears all messages (both system messages and regular messages) from the history.
		/// </summary>
		public void Clear()
		{
			_systemMessages.Clear();
			_messages.Clear();
			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		/// <summary>
		/// Creates a new <see cref="MessageHistory"/> by copying the current instance and adding a specified message.
		/// </summary>
		/// <param name="message">The message to add.</param>
		/// <returns>A new <see cref="MessageHistory"/> with the added message.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
		public MessageHistory With(IMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			return new MessageHistory(this)
			{
				message
			};
		}

		/// <summary>
		/// Creates a new <see cref="MessageHistory"/> by copying the current instance and adding specified messages.
		/// </summary>
		/// <param name="messages">The messages to add.</param>
		/// <returns>A new <see cref="MessageHistory"/> with the added messages.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="messages"/> is null.</exception>
		public MessageHistory With(params IMessage[] messages)
		{
			if (messages == null)
				throw new ArgumentNullException(nameof(messages));

			var history = new MessageHistory(this);
			history.AddRange(messages);
			return history;
		}

		/// <summary>
		/// Creates a new <see cref="MessageHistory"/> by copying the current instance with all system messages
		/// and last <paramref name="count"/> non-system messages.
		/// </summary>
		/// <param name="count">The max count of non-system messages to copy.</param>
		/// <returns>The new <see cref="MessageHistory"/> with the last <paramref name="count"/> non-system messages.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is less than zero.</exception>
		public MessageHistory Last(int count)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			var history = new MessageHistory();
			history.AddRange(_systemMessages);

			if (count >= _messages.Count)
				history.AddRange(_messages);
			else
				history.AddRange(Messages.Skip(_messages.Count - count));

			return history;
		}

		/// <summary>
		/// Throws an exception if there are no non-system messages.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if there are no non-system messages.</exception>
		public void ThrowIfEmpty()
		{
			lock (syncLock)
			{
				if (IsEmpty)
					throw new InvalidOperationException("Message history contains no non-system messages.");
			}
		}

		/// <summary>
		/// Creates a new instance of <see cref="MessageHistory"/> with a single system message.
		/// </summary>
		/// <param name="message">The system message content.</param>
		/// <returns>A new <see cref="MessageHistory"/> with the system message.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
		public static MessageHistory SystemMessage(string message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			return new MessageHistory(new SystemMessage(message));
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="MessageHistory"/> with a single user message.
		/// </summary>
		/// <param name="message">The user message content.</param>
		/// <returns>A new <see cref="MessageHistory"/> with the user message.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
		public static MessageHistory UserMessage(string message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			return new MessageHistory(new UserMessage(message));
		}

		/// <summary>
		/// Creates a new instance of <see cref="MessageHistory"/> with a single message.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <returns>A new <see cref="MessageHistory"/> with the message.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
		public static MessageHistory SingleMessage(IMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			return new MessageHistory(message);
		}

		/// <summary>
		/// Creates a new instance of <see cref="MessageHistory"/> with a pair of system and user messages.
		/// </summary>
		/// <param name="system">The system message.</param>
		/// <param name="user">The user message.</param>
		/// <returns>A new <see cref="MessageHistory"/> with the messages.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="system"/> or <paramref name="user"/> is null.</exception>
		public static MessageHistory SystemUserPair(string system, string user)
		{
			if (system == null)
				throw new ArgumentNullException(nameof(system));
			if (user == null)
				throw new ArgumentNullException(nameof(user));

			return new MessageHistory(new SystemMessage(system), new UserMessage(user));
		}

		/// <summary>
		/// Creates a new instance of <see cref="MessageHistory"/> with a tuple of system, user and assistant messages.
		/// </summary>
		/// <param name="system">The system message.</param>
		/// <param name="user">The user message.</param>
		/// <param name="assistant">The assistant message.</param>
		/// <returns>A new <see cref="MessageHistory"/> with the messages.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="system"/> or <paramref name="user"/> is null.</exception>
		public static MessageHistory SystemUserAssistant(string system, string user, string assistant)
		{
			if (system == null)
				throw new ArgumentNullException(nameof(system));
			if (user == null)
				throw new ArgumentNullException(nameof(user));
			if (assistant == null)
				throw new ArgumentNullException(nameof(assistant));

			return new MessageHistory(new SystemMessage(system), new UserMessage(user), new AssistantMessage(assistant));
		}

		/// <summary>
		/// Creates a new instance of <see cref="MessageHistory"/> with a tuple of system and two user messages.
		/// </summary>
		/// <param name="system">The system message.</param>
		/// <param name="user1">The user message 1.</param>
		/// <param name="user2">The user message 2.</param>
		/// <returns>A new <see cref="MessageHistory"/> with the messages.</returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="system"/>, <paramref name="user1"/> or <paramref name="user2"/> is null.</exception>
		public static MessageHistory SystemUserUser(string system, string user1, string user2)
		{
			if (system == null)
				throw new ArgumentNullException(nameof(system));
			if (user1 == null)
				throw new ArgumentNullException(nameof(user1));
			if (user2 == null)
				throw new ArgumentNullException(nameof(user2));

			return new MessageHistory(new SystemMessage(system), new UserMessage(user1), new UserMessage(user2));
		}

		public IEnumerator<IMessage> GetEnumerator()
		{
			return GetMessages().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}