using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Messages.Attachments;
using RCLargeLanguageModels.Metadata;
using RCLargeLanguageModels.Tasks;
using RCLargeLanguageModels.Tools;

namespace RCLargeLanguageModels.Messages
{
	/// <summary>
	/// Represents a partial AI assitant message that is received from streaming chat completions.
	/// </summary>
	public class PartialAssistantMessage : AsyncEnumerableBase<AssistantMessageDelta>,
		IAssistantMessage,
		INotifyPropertyChanged
	{
		private readonly object _lock = new object();

		private readonly StringBuilder _content;
		private readonly StringBuilder _reasoningContent;
		private readonly List<IToolCall> _toolCalls;
		private readonly List<IAttachment> _attachments;
		private readonly List<IMetadata> _partialMetadata;
		private MetadataCollection _metadata;

		public Role Role => Role.Assistant;

		/// <summary>
		/// Gets the count of stored deltas.
		/// </summary>
		public int DeltasCount => base.Count;

		/// <summary>
		/// Gets the completion token that can be used to track message's completion state.
		/// </summary>
		public new CompletionToken CompletionToken => base.CompletionToken;

		/// <summary>
		/// The partial contents of the streaming assistant message.
		/// </summary>
		public string Content
		{
			get
			{
				lock (_lock)
					return _content.ToString();
			}
		}

		/// <summary>
		/// The partial reasoning contents of the streaming assitant message.
		/// </summary>
		public string ReasoningContent
		{
			get
			{
				lock (_lock)
					return _reasoningContent.ToString();
			}
		}

		/// <summary>
		/// The currently stored tool calls.
		/// </summary>
		public IReadOnlyList<IToolCall> ToolCalls { get; }

		/// <summary>
		/// The currently stored attachments.
		/// </summary>
		public IReadOnlyList<IAttachment> Attachments { get; }

		/// <summary>
		/// Gets the currently stored partial metadata (such as token logprobs: <see cref="ITokenProbabilitiesMetadata"/>).
		/// </summary>
		public IReadOnlyList<IMetadata> PartialMetadata { get; }

		/// <summary>
		/// The completion metadata (such as stop reason: <see cref="IStopReasonMetadata"/>). Will be empty until completed.
		/// </summary>
		public MetadataCollection Metadata => _metadata;
		IMetadataCollection IMetadataProvider.Metadata => Metadata;
		IMetadataCollection ICompletion.Metadata => Metadata;

		/// <summary>
		/// Gets the currently stored token probabilities metadata (known as logprobs), may be empty.
		/// </summary>
		public IEnumerable<ITokenProbabilitiesMetadata> TokenProbabilities
			=> PartialMetadata.OfType<ITokenProbabilitiesMetadata>();

		/// <summary>
		/// Gets the stop reason metadata that caused the generation end, may be <see langword="null"/> (especially if not completed).
		/// </summary>
		public IStopReasonMetadata? StopReason => Metadata?.TryGet<IStopReasonMetadata>();

		/// <summary>
		/// The event is raised when a new part of the assistant message is added to message.
		/// </summary>
		public event EventHandler<AssistantMessageDelta> PartAdded;

		/// <summary>
		/// The event that is raised when message completes (successfully, cancelled or failed).
		/// </summary>
		public event EventHandler<CompletedEventArgs> Completed
		{
			add => base.CompletionToken.Completed += value;
			remove => base.CompletionToken.Completed -= value;
		}

		/// <summary>
		/// The event is raised when a property of the partial assistant message is changed.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;



		/// <summary>
		/// Creates an empty instance of <see cref="PartialAssistantMessage"/> class.
		/// </summary>
		public PartialAssistantMessage()
			: this(null, null, null, null, null, CompletionState.Incomplete, null, null)
		{
		}

		/// <summary>
		/// Creates a new instance of <see cref="PartialAssistantMessage"/> class using the specified parameters.
		/// </summary>
		/// <remarks>
		/// Created message will be considered as succeed completed. All parameters can be <see langword="null"/>.
		/// </remarks>
		/// <param name="content">The main text content.</param>
		/// <param name="reasoningContent">The model reasoning content.</param>
		/// <param name="toolCalls">The list of tool calls.</param>
		/// <param name="attachments">The list of attachments.</param>
		/// <param name="partialMetadata">The list of partial metadata.</param>
		/// <param name="completionMetadata">The completion metadata.</param>
		public PartialAssistantMessage(
			string? content = null,
			string? reasoningContent = null,
			IEnumerable<IToolCall>? toolCalls = null,
			IEnumerable<IAttachment>? attachments = null,
			IEnumerable<IMetadata>? partialMetadata = null,
			IEnumerable<IMetadata>? completionMetadata = null)
			: this(
				  content,
				  reasoningContent,
				  toolCalls,
				  attachments,
				  partialMetadata,
				  CompletionState.Success,
				  completionMetadata, null)
		{
		}

		/// <summary>
		/// Creates a new instance of <see cref="PartialAssistantMessage"/> class using the specified parameters.
		/// </summary>
		/// <remarks>
		/// All parameters can be <see langword="null"/> or <see langword="default"/>.
		/// </remarks>
		/// <param name="content">The main text content.</param>
		/// <param name="reasoningContent">The model reasoning content.</param>
		/// <param name="toolCalls">The list of tool calls.</param>
		/// <param name="attachments">The list of attachments.</param>
		/// <param name="partialMetadata">The list of partial metadata.</param>
		/// <param name="completionState">The completion state of the partial assistant message.</param>
		/// <param name="completionMetadata">
		/// The completion metadata (such as stop reason: <see cref="IStopReasonMetadata"/>).
		/// Will be ignored when <paramref name="completionState"/>==<see cref="CompletionState.Incomplete"/>.
		/// </param>
		/// <param name="completionException">
		/// The exception that caused the <paramref name="completionState"/>=<see cref="CompletionState.Failed"/>.
		/// Must me non-<see langword="null"/> when using with failed completion state.
		/// </param>
		public PartialAssistantMessage(
			string? content = null,
			string? reasoningContent = null,
			IEnumerable<IToolCall>? toolCalls = null,
			IEnumerable<IAttachment>? attachments = null,
			IEnumerable<IMetadata>? partialMetadata = null,
			CompletionState completionState = CompletionState.Incomplete,
			IEnumerable<IMetadata>? completionMetadata = null,
			Exception? completionException = null)
			: base(
				GetInitialDelta(content, reasoningContent, toolCalls, attachments, partialMetadata),
				completionState,
				completionException)
		{
			_content = new StringBuilder(content);
			_reasoningContent = new StringBuilder(reasoningContent);
			_toolCalls = toolCalls?.ToList() ?? new List<IToolCall>();
			_attachments = attachments?.ToList() ?? new List<IAttachment>();
			_partialMetadata = partialMetadata?.ToList() ?? new List<IMetadata>();

			if (completionMetadata != null && completionState != CompletionState.Incomplete)
				_metadata = new MetadataCollection(completionMetadata);
			else
				_metadata = MetadataCollection.Empty;

			ToolCalls = _toolCalls.AsReadOnly();
			Attachments = _attachments.AsReadOnly();
			PartialMetadata = _partialMetadata.AsReadOnly();
		}

		private static IEnumerable<AssistantMessageDelta> GetInitialDelta(
			string? content,
			string? reasoningContent,
			IEnumerable<IToolCall>? toolCalls,
			IEnumerable<IAttachment>? attachments,
			IEnumerable<IMetadata>? partialMetadata)
		{
			if (string.IsNullOrEmpty(content)
				&& string.IsNullOrEmpty(reasoningContent)
				&& toolCalls.IsNullOrEmpty()
				&& attachments.IsNullOrEmpty()
				&& partialMetadata.IsNullOrEmpty())
				return Enumerable.Empty<AssistantMessageDelta>();

			return new AssistantMessageDelta[]
			{
				new AssistantMessageDelta(content, reasoningContent, toolCalls, attachments, partialMetadata)
			};
		}

		/// <summary>
		/// Adds a delta part of the assistant message. If delta is empty, it will ignored.
		/// </summary>
		/// <param name="deltaContent">The delta of main text content.</param>
		/// <param name="deltaReasoningContent">The delta of reasoning text content.</param>
		/// <param name="newToolCalls">The new tool calls.</param>
		/// <param name="newAttachments">The new attachments.</param>
		/// <param name="newPartialMetadata">The new partial metadata.</param>
		public void Add(
			string deltaContent = null,
			string deltaReasoningContent = null,
			IEnumerable<IToolCall> newToolCalls = null,
			IEnumerable<IAttachment> newAttachments = null,
			IEnumerable<IMetadata> newPartialMetadata = null)
		{
			Add
			(
				new AssistantMessageDelta
				(
					deltaContent,
					deltaReasoningContent,
					newToolCalls,
					newAttachments,
					newPartialMetadata
				)
			);
		}

		/// <summary>
		/// Adds a delta part to the assistant message. If delta is empty, it will ignored.
		/// </summary>
		/// <param name="delta">The delta of the assistant message.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="delta"/> is <see langword="null"/>.</exception>
		public new void Add(AssistantMessageDelta delta)
		{
			if (delta == null)
				throw new ArgumentNullException(nameof(delta));
			if (delta.IsEmpty)
				return;

			CompletionToken.ThrowIfComplete();

			lock (_lock)
			{
				if (!string.IsNullOrEmpty(delta.DeltaContent))
				{
					_content.Append(delta.DeltaContent);
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Content)));
				}

				if (!string.IsNullOrEmpty(delta.DeltaReasoningContent))
				{
					_reasoningContent.Append(delta.DeltaReasoningContent);
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ReasoningContent)));
				}

				if (!delta.NewToolCalls.IsNullOrEmpty())
				{
					_toolCalls.AddRange(delta.NewToolCalls);
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ToolCalls)));
				}

				if (!delta.NewAttachments.IsNullOrEmpty())
				{
					_attachments.AddRange(delta.NewAttachments);
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Attachments)));
				}

				if (!delta.NewPartialMetadata.IsNullOrEmpty())
				{
					_partialMetadata.AddRange(delta.NewPartialMetadata);
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PartialMetadata)));
				}

				base.Add(delta);
			}

			PartAdded?.Invoke(this, delta);
		}

		private void CompletePrivate(IEnumerable<IMetadata>? completionMetadata)
		{
			CompletionToken.ThrowIfComplete();

			if (completionMetadata != null)
				_metadata = new MetadataCollection(completionMetadata);
			else
				_metadata = MetadataCollection.Empty;

			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Metadata)));
		}

		/// <summary>
		/// Completes partial assistant message with <see cref="CompletionState.Success"/> and provided metadata.
		/// </summary>
		/// <param name="completionMetadata">The optional completion metadata.</param>
		/// <exception cref="InvalidOperationException">Thrown if message is already completed.</exception>
		public void Complete(IEnumerable<IMetadata>? completionMetadata = null)
		{
			lock (_lock)
			{
				CompletePrivate(completionMetadata);
				base.Finish();
			}
		}

		/// <summary>
		/// Completes partial assistant message with <see cref="CompletionState.Cancelled"/> and provided metadata.
		/// </summary>
		/// <param name="completionMetadata">The optional completion metadata.</param>
		/// <exception cref="InvalidOperationException">Thrown if message is already completed.</exception>
		public void Cancel(IEnumerable<IMetadata>? completionMetadata = null)
		{
			lock (_lock)
			{
				CompletePrivate(completionMetadata);
				base.Cancel();
			}
		}

		/// <summary>
		/// Completes partial assistant message with <see cref="CompletionState.Failed"/> and provided exception and metadata.
		/// </summary>
		/// <param name="exception">The exception that caused the message completion failure.</param>
		/// <param name="completionMetadata">The optional completion metadata.</param>
		/// <exception cref="InvalidOperationException">Thrown if message is already completed.</exception>
		public void Fail(Exception exception, IEnumerable<IMetadata>? completionMetadata = null)
		{
			lock (_lock)
			{
				CompletePrivate(completionMetadata);
				base.Fail(exception);
			}
		}

		/// <summary>
		/// Completes partial assistant message with <see cref="CompletedEventArgs"/> and metadata.
		/// </summary>
		/// <param name="args">The completed event args to import from.</param>
		/// <param name="completionMetadata">The optional completion metadata.</param>
		/// <exception cref="InvalidOperationException">Thrown if message is already completed.</exception>
		public void ImportCompletion(CompletedEventArgs args, IEnumerable<IMetadata>? completionMetadata = null)
		{
			lock (_lock)
			{
				CompletePrivate(completionMetadata);
				base.ImportCompletion(args);
			}
		}

		/// <summary>
		/// Gets awaiter for this partial assistant message completion.
		/// </summary>
		/// <returns>The <see cref="CompletionToken"/> that represents the awaiter.</returns>
		public CompletionToken GetAwaiter()
		{
			return CompletionToken;
		}

		/// <summary>
		/// Converts the partial assistant message to a completed assistant message.
		/// </summary>
		/// <returns></returns>
		public AssistantMessage AsAssistantMessage()
		{
			lock (_lock)
			{
				return new AssistantMessage(Content, ReasoningContent, ToolCalls, Attachments, PartialMetadata, Metadata);
			}
		}

		/// <summary>
		/// Creates a copy of the partial assistant message.
		/// </summary>
		/// <returns></returns>
		public PartialAssistantMessage Copy()
		{
			lock (_lock)
			{
				return new PartialAssistantMessage(
					Content,
					ReasoningContent,
					ToolCalls,
					Attachments,
					PartialMetadata,
					CompletionToken.State,
					Metadata,
					CompletionToken.Exception);
			}
		}

		/// <summary>
		/// Creates a copy of the partial assistant message that will be always considered completed.
		/// </summary>
		/// <returns></returns>
		public PartialAssistantMessage CopyCompleted()
		{
			lock (_lock)
			{
				return new PartialAssistantMessage(
					Content,
					ReasoningContent,
					ToolCalls,
					Attachments,
					PartialMetadata,
					Metadata);
			}
		}
	}
}