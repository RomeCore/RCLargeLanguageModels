using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Security
{
	/// <summary>
	/// Interface for accessing tokens.
	/// </summary>
	public interface ITokenAccessor
	{
		/// <summary>
		/// Gets the token.
		/// </summary>
		/// <returns>The token.</returns>
		string GetToken();
	}

	/// <summary>
	/// A token accessor that gets the token from the environment variable.
	/// </summary>
	public class EnvironmentTokenAccessor : ITokenAccessor
	{
		private readonly string _environmentVariableName;

		/// <summary>
		/// Initializes a new instance of the <see cref="EnvironmentTokenAccessor"/> class.
		/// </summary>
		public EnvironmentTokenAccessor(string environmentVariableName)
		{
			_environmentVariableName = environmentVariableName ?? throw new ArgumentNullException(nameof(environmentVariableName));
		}

		public string GetToken()
		{
			return Environment.GetEnvironmentVariable(_environmentVariableName);
		}
	}

	/// <summary>
	/// A token accessor that gets the token from a string.
	/// </summary>
	public class StringTokenAccessor : ITokenAccessor
	{
		private readonly string _token;

		/// <summary>
		/// Initializes a new instance of the <see cref="StringTokenAccessor"/> class.
		/// </summary>
		/// <param name="token">The token.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="token"/> is <see langword="null"/>.</exception>
		public StringTokenAccessor(string token)
		{
			_token = token ?? throw new ArgumentNullException(nameof(token));
		}

		public string GetToken()
		{
			return _token;
		}
	}

	/// <summary>
	/// A token accessor that gets the token from a function.
	/// </summary>
	public class DelegateTokenAccessor : ITokenAccessor
	{
		private readonly Func<string> _getter;

		/// <summary>
		/// Initializes a new instance of the <see cref="DelegateTokenAccessor"/> class.
		/// </summary>
		/// <param name="getter">The getter function.</param>
		public DelegateTokenAccessor(Func<string> getter)
		{
			_getter = getter ?? throw new ArgumentNullException(nameof(getter));
		}

		public string GetToken()
		{
			return _getter.Invoke();
		}
	}
}