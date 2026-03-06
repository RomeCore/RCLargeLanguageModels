using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using RCLargeLanguageModels.Metadata;
using RCLargeLanguageModels.Tasks;

namespace RCLargeLanguageModels.Completions
{
	/// <summary>
	/// Represents a partial/streaming LLM completion result.
	/// </summary>
	public class PartialCompletionResult : ICompletionResult
	{
		private readonly object _lock = new object();
		private readonly CompletionSource _cs;
		private MetadataCollection _metadata;

		/// <summary>
		/// Gets the completion token that can be used to track completion state.
		/// </summary>
		public CompletionToken CompletionToken => _cs.Token;

		/// <summary>
		/// The event that is raised when this partial completion result completes (successfully, cancelled or failed).
		/// </summary>
		public event EventHandler<CompletedEventArgs> Completed
		{
			add => _cs.Completed += value;
			remove => _cs.Completed -= value;
		}

		/// <inheritdoc/>
		public LLMClient Client { get; }

		/// <inheritdoc/>
		public LLModelDescriptor Model { get; }

		/// <summary>
		/// Gets the list of available partial completion choices. Will contain at least one completion.
		/// </summary>
		public ImmutableArray<PartialCompletion> Choices { get; }
		IReadOnlyList<ICompletion> IGenerationResult<ICompletion>.Choices => Choices;

		/// <summary>
		/// Gets the first partial completion from the choices.
		/// </summary>
		public PartialCompletion Completion => Choices[0];
		ICompletion IGenerationResult<ICompletion>.Completion => Completion;

		/// <inheritdoc/>
		public string Content => Completion.Content;

		/// <summary>
		/// Get the completion metadata (such as usage stats: <see cref="IUsageMetadata"/>). Will be empty until completed.
		/// </summary>
		public MetadataCollection Metadata => _metadata;
		IMetadataCollection IMetadataProvider.Metadata => Metadata;
		IMetadataCollection IGenerationResult<ICompletion>.Metadata => Metadata;

		/// <inheritdoc/>
		public IUsageMetadata? UsageMetadata => _metadata.TryGet<IUsageMetadata>();

		/// <summary>
		/// Creates a new instance of <see cref="PartialCompletionResult"/> class using the specified parameters.
		/// </summary>
		/// <remarks>
		/// Created completion result will be considered as succeed completed.
		/// </remarks>
		/// <param name="client">The client that used to generate this completion.</param>
		/// <param name="model">The model descriptor that used to generate this completion.</param>
		/// <param name="choice">The single partial completion choice.</param>
		/// <param name="metadata">
		/// The completion metadata (such as usage stats: <see cref="IUsageMetadata"/>).
		/// </param>
		public PartialCompletionResult(
			LLMClient client,
			LLModelDescriptor model,
			PartialCompletion choice,
			IEnumerable<IMetadata>? metadata = null)
			: this(client, model, choice?.WrapIntoArray(), CompletionState.Success, metadata, null)
		{
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="PartialCompletionResult"/> class using the specified parameters.
		/// </summary>
		/// <remarks>
		/// Created completion result will be considered as succeed completed.
		/// </remarks>
		/// <param name="client">The client that used to generate this completion.</param>
		/// <param name="model">The model descriptor that used to generate this completion.</param>
		/// <param name="choices">
		/// The list of available choices. <br/>
		/// Warning! Once this instance is created,
		/// list of choices cannot be changed, please fill it with partial completions first and interact with them later.
		/// </param>
		/// <param name="metadata">
		/// The completion metadata (such as usage stats: <see cref="IUsageMetadata"/>).
		/// </param>
		public PartialCompletionResult(
			LLMClient client,
			LLModelDescriptor model,
			IEnumerable<PartialCompletion> choices,
			IEnumerable<IMetadata>? metadata = null)
			: this(client, model, choices, CompletionState.Success, metadata, null)
		{
		}

		/// <summary>
		/// Creates a new instance of <see cref="PartialCompletionResult"/> class using the specified parameters.
		/// </summary>
		/// <param name="client">The client that used to generate this completion.</param>
		/// <param name="model">The model descriptor that used to generate this completion.</param>
		/// <param name="choices">
		/// The list of available choices. <br/>
		/// Warning! Once this instance is created,
		/// list of choices cannot be changed, please fill it with partial completions first and interact with them later.
		/// </param>
		/// <param name="completionState">The completion state of the partial completion result.</param>
		/// <param name="metadata">
		/// The completion metadata (such as usage stats: <see cref="IUsageMetadata"/>).
		/// Will be ignored when <paramref name="completionState"/>==<see cref="CompletionState.Incomplete"/>.
		/// </param>
		/// <param name="completionException">
		/// The exception that caused the <paramref name="completionState"/>=<see cref="CompletionState.Failed"/>.
		/// Must me non-<see langword="null"/> when using with failed completion state.
		/// </param>
		public PartialCompletionResult(
			LLMClient client,
			LLModelDescriptor model,
			IEnumerable<PartialCompletion> choices,
			CompletionState completionState,
			IEnumerable<IMetadata>? metadata = null,
			Exception? completionException = null)
		{
			Client = client ?? throw new ArgumentNullException(nameof(client));
			Model = model ?? throw new ArgumentNullException(nameof(model));
			Choices = choices?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(choices));

			if (Choices.Length == 0)
				throw new ArgumentException("Choices must have at least one element.", nameof(choices));
			if (Choices.Any(c => c == null))
				throw new ArgumentNullException(nameof(choices), "One element in choices is null.");

			if (metadata != null && completionState != CompletionState.Incomplete)
				_metadata = new MetadataCollection(metadata);
			else
				_metadata = MetadataCollection.Empty;

			_cs = new CompletionSource(completionState, completionException);
		}

		private void CompletePrivate(IEnumerable<IMetadata>? completionMetadata)
		{
			CompletionToken.ThrowIfComplete();

			if (completionMetadata != null)
				_metadata = new MetadataCollection(completionMetadata);
			else
				_metadata = MetadataCollection.Empty;
		}

		/// <summary>
		/// Completes partial completion result with <see cref="CompletionState.Success"/> using provided optional metadata.
		/// </summary>
		/// <param name="completionMetadata">The optional completion metadata.</param>
		/// <exception cref="InvalidOperationException">Thrown if this instance is already completed.</exception>
		public void Complete(IEnumerable<IMetadata>? completionMetadata = null)
		{
			lock (_lock)
			{
				CompletePrivate(completionMetadata);
				_cs.Complete();
			}
		}

		/// <summary>
		/// Completes partial completion result with <see cref="CompletionState.Cancelled"/> using provided optional metadata.
		/// </summary>
		/// <param name="completionMetadata">The optional completion metadata.</param>
		/// <exception cref="InvalidOperationException">Thrown if this instance is already completed.</exception>
		public void Cancel(IEnumerable<IMetadata>? completionMetadata = null)
		{
			lock (_lock)
			{
				CompletePrivate(completionMetadata);
				_cs.Cancel();
			}
		}

		/// <summary>
		/// Completes partial completion result with <see cref="CompletionState.Failed"/> using provided exception and optional metadata.
		/// </summary>
		/// <param name="exception">The exception that caused the completion failure.</param>
		/// <param name="completionMetadata">The optional completion metadata.</param>
		/// <exception cref="InvalidOperationException">Thrown if this instance is already completed.</exception>
		public void Fail(Exception exception, IEnumerable<IMetadata>? completionMetadata = null)
		{
			lock (_lock)
			{
				CompletePrivate(completionMetadata);
				_cs.Fail(exception);
			}
		}

		/// <summary>
		/// Gets awaiter for this partial completion result.
		/// </summary>
		public CompletionToken GetAwaiter()
		{
			return CompletionToken;
		}
	}
}