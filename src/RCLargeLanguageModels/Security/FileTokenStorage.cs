using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace RCLargeLanguageModels.Security
{
	/// <summary>
	/// Provides secure storage for API tokens in a file with encryption.
	/// </summary>
	public class FileTokenStorage : TokenStorage
	{
		private readonly string StoragePath;
		private readonly string Password;
		private readonly object FileLock = new object();

		private readonly byte[] Salt = new byte[] { 0x26, 0xdc, 0xff, 0x00, 0xad, 0xed, 0x7a, 0xee, 0xc5, 0xfe, 0x07, 0xaf, 0x4d, 0x08, 0x22, 0x3c };
		private readonly byte[] EncryptionKey;

		public FileTokenStorage(string filepath, string password)
		{
			StoragePath = Path.GetFullPath(filepath);
			Password = password;
			EncryptionKey = GetEncryptionKey();
		}

		public override bool CanWrite => true;

		/// <summary>
		/// Stores an API token with encryption
		/// </summary>
		/// <param name="name">Service name identifier</param>
		/// <param name="token">Plaintext API token</param>
		protected override void WriteTokenOverride(string name, string token)
		{
			lock (FileLock)
			{
				var tokens = LoadTokens();
				var encryptedData = EncryptToken(token);
				tokens[name] = encryptedData;
				SaveTokens(tokens);
			}
		}

		/// <summary>
		/// Retrieves a decrypted API token
		/// </summary>
		/// <param name="name">Service name identifier</param>
		/// <returns>Decrypted token or null if not found</returns>
		protected override string GetTokenOverride(string name)
		{
			lock (FileLock)
			{
				var tokens = LoadTokens();
				return tokens.TryGetValue(name, out var encryptedData)
					? DecryptToken(encryptedData, name)
					: null;
			}
		}

		/// <summary>
		/// Imports tokens from a plaintext file and encrypts them
		/// </summary>
		/// <param name="filePath">Path to plaintext token file</param>
		public void LoadFromTextFile(string filePath)
		{
			lock (FileLock)
			{
				var rawTokens = new Dictionary<string, string>();
				foreach (var line in File.ReadAllLines(filePath))
				{
					var parts = line.Split(new[] { ':' }, 2);
					if (parts.Length == 2)
						rawTokens[parts[0].Trim()] = parts[1].Trim();
				}

				var existingTokens = LoadTokens();
				foreach (var kvp in rawTokens)
				{
					existingTokens[kvp.Key] = EncryptToken(kvp.Value);
				}

				SaveTokens(existingTokens);
			}
		}

		private EncryptedData EncryptToken(string token)
		{
			using (var aes = Aes.Create())
			{
				aes.Key = EncryptionKey;
				aes.GenerateIV();

				using (var encryptor = aes.CreateEncryptor())
				using (var ms = new MemoryStream())
				{
					using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
					using (var writer = new StreamWriter(cs))
					{
						writer.Write(token);
					}

					return new EncryptedData
					{
						IV = Convert.ToBase64String(aes.IV),
						CipherText = Convert.ToBase64String(ms.ToArray())
					};
				}
			}
		}

		private string DecryptToken(EncryptedData data, string name)
		{
			try
			{
				using (var aes = Aes.Create())
				{
					aes.Key = EncryptionKey;
					aes.IV = Convert.FromBase64String(data.IV);

					using (var decryptor = aes.CreateDecryptor())
					using (var ms = new MemoryStream(Convert.FromBase64String(data.CipherText)))
					using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
					using (var reader = new StreamReader(cs))
					{
						var str = reader.ReadToEnd();
						return str;
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to decrypt token for {name}", name);
				return null;
			}
		}

		private Dictionary<string, EncryptedData> LoadTokens()
		{
			if (!File.Exists(StoragePath))
				return new Dictionary<string, EncryptedData>();

			var json = File.ReadAllText(StoragePath);
			return JsonConvert.DeserializeObject<Dictionary<string, EncryptedData>>(json);
		}

		private void SaveTokens(Dictionary<string, EncryptedData> tokens)
		{
			var json = JsonConvert.SerializeObject(tokens, Formatting.Indented);
			File.WriteAllText(StoragePath, json);
		}

		private class EncryptedData
		{
			public string IV { get; set; }
			public string CipherText { get; set; }
		}

		private byte[] GetEncryptionKey()
		{
			using (var deriveBytes = new Rfc2898DeriveBytes(Password, salt: Salt, iterations: 500000))
				return deriveBytes.GetBytes(32);
		}
	}
}