using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels
{
	/// <summary>
	/// The LLM provider that simply returns a LLM.
	/// </summary>
	public interface ILLMProvider
	{
		/// <summary>
		/// Gets LLM instance.
		/// </summary>
		LLModel GetLLM();
	}
}