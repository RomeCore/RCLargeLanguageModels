using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Security
{
	public class DictionaryTokenStorage : TokenStorage
	{
		private readonly Dictionary<string, string> _tokens = new Dictionary<string, string>();

		public override bool CanWrite => true;

		protected override string GetTokenOverride(string name)
		{
			if (_tokens.TryGetValue(name, out string token))
			{
				return token;
			}

			return null;
		}

		protected override void WriteTokenOverride(string name, string token)
		{
			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentNullException(nameof(name), "Token name cannot be null or empty");
			}

			if (_tokens.ContainsKey(name))
			{
				_tokens[name] = token;
			}
			else
			{
				_tokens.Add(name, token);
			}
		}
	}
}