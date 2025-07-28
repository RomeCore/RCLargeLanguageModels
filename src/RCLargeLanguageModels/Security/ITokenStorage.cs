using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Security
{
	/// <summary>
	/// Interface for retrieving API tokens.
	/// </summary>
	public interface ITokenStorage
	{
		/// <summary>
		/// Writes an API token.
		/// </summary>
		bool CanWrite { get; }

		/// <summary>
		/// Stores an API token.
		/// </summary>
		/// <remarks>
		/// Will throw an exception if the storage is read-only.
		/// </remarks>
		/// <param name="name">The token name.</param>
		/// <param name="token">The token value.</param>
		void WriteToken(string name, string token);

		/// <summary>
		/// Tries to get the API token for the given name.
		/// </summary>
		/// <remarks>
		/// Some implementations may check if this method is called securely.
		/// </remarks>
		/// <param name="name">The name of token.</param>
		/// <returns>The raw token value or null if not found.</returns>
		string GetToken(string name);

		/// <summary>
		/// Gets the API token accessor for the given name.
		/// </summary>
		/// <remarks>
		/// When accessor is retrieved, another accessor for the same name cannot be retrieved.
		/// </remarks>
		/// <param name="name">The name of token.</param>
		/// <returns>The accessor for the token.</returns>
		ITokenAccessor GetAccessor(string name);
	}

	/// <summary>
	/// A token storage that does nothing. Used by default in <see cref="TokenStorage"/> class.
	/// </summary>
	public class DummyTokenStorage : ITokenStorage
	{
		public bool CanWrite => false;

		public void WriteToken(string name, string token)
		{
			throw new NotImplementedException();
		}

		public string GetToken(string name)
		{
			return null;
		}

		public ITokenAccessor GetAccessor(string name)
		{
			return new DelegateTokenAccessor(() => null);
		}
	}
}