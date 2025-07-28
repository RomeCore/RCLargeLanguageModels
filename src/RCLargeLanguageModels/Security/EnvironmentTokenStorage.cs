using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Security
{
	public class EnvironmentTokenStorage : TokenStorage
	{
		private Dictionary<string, string> _map = new Dictionary<string, string>();

		public override bool CanWrite => false;

		/// <summary>
		/// Initializes a new instance of the <see cref="EnvironmentTokenStorage"/> class using the specified map.
		/// </summary>
		/// <param name="map">The token map where the key is token name and the value is the environment variable name.</param>
		/// <exception cref="ArgumentNullException"></exception>
		public EnvironmentTokenStorage(Dictionary<string, string> map)
		{
			_map = map ?? throw new ArgumentNullException(nameof(map));
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="EnvironmentTokenStorage"/> class using the specified map.
		/// </summary>
		/// <param name="map">The token map where the key is token name and the value is the environment variable name.</param>
		/// <exception cref="ArgumentNullException"></exception>
		public EnvironmentTokenStorage(params KeyValuePair<string, string>[] map)
		{
			_map = map?.ToDictionary(m => m.Key, m => m.Value) ?? throw new ArgumentNullException(nameof(map));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="EnvironmentTokenStorage"/> class using the specified map.
		/// </summary>
		/// <param name="map">The token map where the first value is token name and the second value is the environment variable name.</param>
		/// <exception cref="ArgumentNullException"></exception>
		public EnvironmentTokenStorage(params (string, string)[] map)
		{
			_map = map?.ToDictionary(m => m.Item1, m => m.Item2) ?? throw new ArgumentNullException(nameof(map));
		}

		protected override void WriteTokenOverride(string name, string token)
		{
			if (!_map.ContainsKey(name))
				return;
			var envname = _map[name];
			Environment.SetEnvironmentVariable(envname, token);
		}

		protected override string GetTokenOverride(string name)
		{
			if (!_map.ContainsKey(name))
				return null;
			var envname = _map[name];
			return Environment.GetEnvironmentVariable(envname);
		}
	}
}