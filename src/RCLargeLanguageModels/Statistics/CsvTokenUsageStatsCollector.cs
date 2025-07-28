using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Statistics
{
	/// <summary>
	/// Represents a token usage stats collector that writes the token usage statistics to a CSV file.
	/// </summary>
	public class CsvTokenUsageStatsCollector : ITokenUsageStatsCollector
	{
		private string _filePath;

		/// <summary>
		/// Initializes a new instance of the <see cref="CsvTokenUsageStatsCollector"/> class.
		/// </summary>
		/// <param name="filePath">The path to the CSV file to write the token usage statistics.</param>
		public CsvTokenUsageStatsCollector(string filePath)
		{
			_filePath = Path.GetFullPath(filePath);
		}

		public void AppendUsage(string userName, string clientName, string modelName, int inputTokens, int outputTokens)
		{
			if (userName == null)
				throw new ArgumentNullException(nameof(userName));
			if (clientName == null)
				throw new ArgumentNullException(nameof(clientName));
			if (modelName == null)
				throw new ArgumentNullException(nameof(modelName));

			if (!File.Exists(_filePath))
				File.WriteAllText(_filePath, "Timestamp;User;Client;Model;Input tokens;Output tokens\n");

			var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
			var line = $"{timestamp};{userName};{clientName};{modelName};{inputTokens};{outputTokens}\n";
			File.AppendAllText(_filePath, line);
		}
	}
}