using System;

namespace RCLargeLanguageModels.Exceptions
{
	/// <summary>
	/// Represents an exception that is thrown when a user attempts to access a resource without proper authorization.
	/// </summary>
	public class NonAuthorizedException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NonAuthorizedException"/> class with a specified error message.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		public NonAuthorizedException(string message) : base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NonAuthorizedException"/> class with a specified error message and a reference to the inner exception that caused this exception.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		/// <param name="innerException">The exception that caused this exception. If no inner exception is specified, the default value is <see langword="null"/>.</param>
		public NonAuthorizedException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}