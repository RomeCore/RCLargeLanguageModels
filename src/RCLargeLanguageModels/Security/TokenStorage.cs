using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace RCLargeLanguageModels.Security
{
	/// <summary>
	/// Provides secure storage for API tokens.
	/// </summary>
	public abstract class TokenStorage : ITokenStorage
	{
		private static readonly ITokenStorage _defaultTokenStorage = new DummyTokenStorage();

		/// <summary>
		/// The shared token storage.
		/// </summary>
		public static ITokenStorage Shared { get; set; }

		/// <summary>
		/// Writes a token to the shared storage.
		/// </summary>
		/// <param name="name">The token name.</param>
		/// <param name="token">The token value.</param>
		/// <exception cref="InvalidOperationException">Thrown if the shared storage is not set or it is read-only.</exception>
		public static void WriteTokenShared(string name, string token)
		{
			if (Shared == null)
				throw new InvalidOperationException("The shared token storage is not set.");

			if (!Shared.CanWrite)
				throw new InvalidOperationException("The shared token storage is read-only.");

			Shared.WriteToken(name, token);
		}

		/// <summary>
		/// Gets a token from the shared storage.
		/// </summary>
		/// <param name="name">The token name.</param>
		/// <returns>The token value.</returns>
		public static string GetTokenShared(string name)
		{
			if (Shared == null)
				return _defaultTokenStorage.GetToken(name);

			return Shared.GetToken(name);
		}

		/// <summary>
		/// Gets a token accessor for the given name using a shared token storage.
		/// </summary>
		/// <param name="name">The name of the token.</param>
		/// <returns>The accessor for the given name.</returns>
		/// <exception cref="InvalidOperationException"></exception>
		public static ITokenAccessor GetAccessorShared(string name)
		{
			if (Shared == null)
				return _defaultTokenStorage.GetAccessor(name);

			return Shared.GetAccessor(name);
		}

		// ================
		// INSTANCE METHODS
		// ================

		public abstract bool CanWrite { get; }

		public void WriteToken(string name, string token)
		{
			if (!CanWrite)
				throw new InvalidOperationException("The storage is read-only.");

			WriteTokenOverride(name, token);
		}

		public string GetToken(string name)
		{
			return GetTokenOverride(name);
		}

		public ITokenAccessor GetAccessor(string name)
		{
			return new DelegateTokenAccessor(() => GetToken(name));
		}

		/// <summary>
		/// Writes the token value to the storage.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="token"></param>
		protected abstract void WriteTokenOverride(string name, string token);

		/// <summary>
		/// Gets the token value from the storage.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		protected abstract string GetTokenOverride(string name);
	}
}