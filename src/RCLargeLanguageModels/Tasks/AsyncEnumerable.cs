using System;
using System.Collections.Generic;

namespace RCLargeLanguageModels.Tasks
{
	/// <summary>
	/// Provides a concrete implementation of <see cref="AsyncEnumerableBase{T}"/> with public Add/Finish methods.
	/// This class maintains all values in memory and allows multiple enumerations over the same data.
	/// </summary>
	/// <typeparam name="T">The type of elements to enumerate.</typeparam>
	/// <remarks>
	/// Inherits all functionality from <see cref="AsyncEnumerableBase{T}"/> while exposing
	/// the protected methods as public for easier consumption.
	/// </remarks>
	public class AsyncEnumerable<T> : AsyncEnumerableBase<T>
	{
		/// <summary>
		/// The completion token thet gives info about completion fact and the completion state.
		/// </summary>
		public new CompletionToken CompletionToken => base.CompletionToken;

		/// <summary>
		/// Gets the vlue indicating whether enumerable is completed.
		/// </summary>
		public new bool IsCompleted => base.IsCompleted;

		/// <summary>
		/// Gets the read-only list containing current completed values.
		/// </summary>
		public new IReadOnlyList<T> Values => base.Values;

		/// <summary>
		/// Gets the count of currently completed values.
		/// </summary>
		public new int Count => base.Count;

		/// <summary>
		/// Initializes a new empty non-completed <see cref="AsyncEnumerable{T}"/>.
		/// </summary>
		public AsyncEnumerable()
			: base()
		{
		}

		/// <summary>
		/// Initializes a new non-completed <see cref="AsyncEnumerable{T}"/> with pre-populated values.
		/// </summary>
		/// <param name="completedValues">Values that are immediately available.</param>
		/// <exception cref="ArgumentNullException">Thrown if completedValues is null.</exception>
		public AsyncEnumerable(IEnumerable<T> completedValues)
			: base(completedValues)
		{
		}

		/// <summary>
		/// Initializes a new <see cref="AsyncEnumerable{T}"/> with pre-populated values and completion state.
		/// </summary>
		/// <param name="completedValues">Values that are immediately available.</param>
		/// <param name="isFinished">The initial state marking enumerable completion.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="completedValues"/> is null.</exception>
		public AsyncEnumerable(IEnumerable<T> completedValues, bool isFinished)
			: base(completedValues, isFinished)
		{
		}

		/// <summary>
		/// Initializes a new <see cref="AsyncEnumerable{T}"/> with pre-populated values and completion state with optional completion exception.
		/// </summary>
		/// <param name="completedValues">Values that are immediately available.</param>
		/// <param name="state">The initial state marking enumerable completion.</param>
		/// <param name="exception">
		/// The exception for <see cref="CompletionState.Failed"/> completion state.
		/// Gives info about what caused the enumerable to finish.
		/// </param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="completedValues"/> is null.</exception>
		public AsyncEnumerable(IEnumerable<T> completedValues, CompletionState state, Exception? exception = null)
			: base(completedValues, state, exception)
		{
		}

		/// <summary>
		/// Adds a new value to the enumeration.
		/// </summary>
		/// <param name="value">The value to add</param>
		/// <exception cref="InvalidOperationException">Thrown if enumeration is finished.</exception>
		public new void Add(T value) => base.Add(value);

		/// <summary>
		/// Adds a new values to the enumeration.
		/// </summary>
		/// <param name="values">The values to add.</param>
		/// <exception cref="InvalidOperationException">Thrown if enumeration is finished.</exception>
		public new void AddRange(IEnumerable<T> values) => base.AddRange(values);

		/// <summary>
		/// Marks the enumeration as complete without adding a final value.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if enumeration is already finished.</exception>
		public new void Finish() => base.Finish();

		/// <summary>
		/// Marks the enumeration as complete and adds a final value.
		/// </summary>
		/// <param name="value">The final value to add</param>
		/// <exception cref="InvalidOperationException">Thrown if enumeration is already finished.</exception>
		public new void Finish(T value) => base.Finish(value);

		/// <summary>
		/// Completes the enumeration as cancelled.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if enumeration is already finished.</exception>
		public new void Cancel() => base.Cancel();

		/// <summary>
		/// Completes the enumeration as faulted.
		/// </summary>
		/// <param name="exception">The exception that caused the failure.</param>
		/// <exception cref="InvalidOperationException">Thrown if enumeration is already finished.</exception>
		public new void Fail(Exception exception) => base.Fail(exception);

		/// <summary>
		/// Imports the completion from the <see cref="CompletedEventArgs"/>.
		/// </summary>
		/// <param name="args">The completed event args to import from.</param>
		public new void ImportCompletion(CompletedEventArgs args) => base.ImportCompletion(args);
	}
}