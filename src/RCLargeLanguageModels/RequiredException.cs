using System;
using System.Collections.Generic;
using System.Text;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Metadata;

namespace RCLargeLanguageModels
{
	/// <summary>
	/// The exception that should be thrown when required object is not available.
	/// </summary>
	public class RequiredException : Exception
	{
		/// <summary>
		/// The type of required object. Can be <see langword="null"/>.
		/// </summary>
		public Type? RequiredType { get; }

		/// <summary>
		/// Creates a new instance of <see cref="RequiredException"/>.
		/// </summary>
		public RequiredException() : base()
		{
		}

		/// <summary>
		/// Creates a new instance of <see cref="RequiredException"/> using specified message.
		/// </summary>
		public RequiredException(string message) : base(message)
		{
		}

		/// <summary>
		/// Creates a new instance of <see cref="RequiredException"/> using specified required type and message.
		/// </summary>
		public RequiredException(Type requiredType, string message) : base(FormatMessage(requiredType, message))
		{
			RequiredType = requiredType;
		}

		private static string FormatMessage(Type requiredType, string message)
		{
			return message.TrimEnd() + $" (required type: {requiredType})";
		}
	}
}