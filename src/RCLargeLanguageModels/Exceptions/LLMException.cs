using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Exceptions
{
	/// <summary>
	/// Represents an exception that is thrown when an error occurs during the execution of a large language model.
	/// </summary>
	public class LLMException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LLMException"/> class with a specified error message.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		public LLMException(string message) : base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LLMException"/> class with a specified error message and inner exception.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="innerException">The exception that is the cause of the current exception.</param>
		public LLMException(string message, Exception innerException) : base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LLMException"/> class with a specified error message and LLM client.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="client">The LLM client that associated with the exception.</param>
		public LLMException(string message, LLMClient client) : base(GetMessage(message, client))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LLMException"/> class with a specified error message, LLM client and inner exception.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="client">The LLM client that associated with the exception.</param>
		/// <param name="innerException">The exception that is the cause of the current exception.</param>
		public LLMException(string message, LLMClient client, Exception innerException) : base(GetMessage(message, client), innerException)
		{
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="LLMException"/> class with a specified error message and model descriptor.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="descriptor">The model descriptor that associated with the exception.</param>
		public LLMException(string message, LLModelDescriptor descriptor) : base(GetMessage(message, descriptor))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LLMException"/> class with a specified error message, model descriptor and inner exception.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="descriptor">The model descriptor that associated with the exception.</param>
		/// <param name="innerException">The exception that is the cause of the current exception.</param>
		public LLMException(string message, LLModelDescriptor descriptor, Exception innerException) : base(GetMessage(message, descriptor), innerException)
		{
		}

		private static string GetMessage(string message, LLMClient client)
		{
			return $"{message}\nClient: {client}";
		}

		private static string GetMessage(string message, LLModelDescriptor descriptor)
		{
			return $"{message}\nModel: {descriptor}";
		}
	}
}