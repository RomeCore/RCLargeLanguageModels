using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates
{
	/// <summary>
	/// Exception thrown when a template encounters an error during execution.
	/// </summary>
	public class TemplateRuntimeException : Exception
	{
		/// <summary>
		/// The data accessor that was being used when the exception occurred. Can be null if no data accessor was in use.
		/// </summary>
		public TemplateDataAccessor? DataAccessor { get; }

		/// <summary>
		/// The expression node that caused the error. Can be null if no specific node was involved.
		/// </summary>
		public TemplateExpressionNode? ExpressionNode { get; }

		/// <summary>
		/// The prompt template that was being executed when the exception occurred. Can be null if no specific template node was involved.
		/// </summary>
		public TextTemplateNode? PromptTemplateNode { get; }

		/// <summary>
		/// The messages template node that was being executed when the exception occurred. Can be null if no specific node was involved.
		/// </summary>
		public MessagesTemplateNode? MessagesTemplateNode { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateRuntimeException"/> class with the specified error message.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		public TemplateRuntimeException(string? message) : base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateRuntimeException"/> class with the specified error message.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
		public TemplateRuntimeException(string? message, Exception? innerException) : base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateRuntimeException"/> class with the specified error message and data.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="dataAccessor">The data accessor that was being used when the exception occurred. Can be null if no data accessor was in use.</param>
		/// <param name="expressionNode">The expression node that caused the error. Can be null if no specific node was involved.</param>
		/// <param name="promptTemplateNode">The prompt template that was being executed when the exception occurred. Can be null if no specific template node was involved.</param>
		/// <param name="messagesTemplateNode">The messages template node that was being executed when the exception occurred. Can be null if no specific node was involved.</param>
		public TemplateRuntimeException(
			string? message,
			TemplateDataAccessor? dataAccessor = null,
			TemplateExpressionNode? expressionNode = null,
			TextTemplateNode? promptTemplateNode = null,
			MessagesTemplateNode? messagesTemplateNode = null
			) : base(message)
		{
			DataAccessor = dataAccessor;
			ExpressionNode = expressionNode;
			PromptTemplateNode = promptTemplateNode;
			MessagesTemplateNode = messagesTemplateNode;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateRuntimeException"/> class with the specified error message and data.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
		/// <param name="dataAccessor">The data accessor that was being used when the exception occurred. Can be null if no data accessor was in use.</param>
		/// <param name="expressionNode">The expression node that caused the error. Can be null if no specific node was involved.</param>
		/// <param name="promptTemplateNode">The prompt template that was being executed when the exception occurred. Can be null if no specific template node was involved.</param>
		/// <param name="messagesTemplateNode">The messages template node that was being executed when the exception occurred. Can be null if no specific node was involved.</param>
		public TemplateRuntimeException(
			string? message,
			Exception? innerException,
			TemplateDataAccessor? dataAccessor = null,
			TemplateExpressionNode? expressionNode = null,
			TextTemplateNode? promptTemplateNode = null,
			MessagesTemplateNode? messagesTemplateNode = null
			) : base(message, innerException)
		{
			DataAccessor = dataAccessor;
			ExpressionNode = expressionNode;
			PromptTemplateNode = promptTemplateNode;
			MessagesTemplateNode = messagesTemplateNode;
		}
	}
}