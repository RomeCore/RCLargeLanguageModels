using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCLargeLanguageModels.Completions;

namespace RCLargeLanguageModels.Clients.Novita
{
	public class NovitaCompletionProperties : ICompletionProperties
	{
		public float? Temperature { get; set; }
		public float? TopP { get; set; }
		public StopSequenceCollection Stop { get; set; }
		public int? MaxTokens { get; set; }
	}
}