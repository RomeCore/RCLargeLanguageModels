using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace RCLargeLanguageModels.Tools
{
	/// <summary>
	/// The utility class for generating unique tool call ids.
	/// </summary>
	public static class ToolCallId
	{
		/// <summary>
		/// Generates a new tool call id string in OpenAI-like format using optional tool call index.
		/// </summary>
		/// <returns>
		/// String in format "call_{index}_{guid}" where guid generates randomly. <br/>
		/// Example: "call_0_abc123de-f456-789a-bc12-345def678abc".
		/// </returns>
		public static string Generate(int index = 0)
		{
			return $"call_{index}_{Guid.NewGuid()}";
		}
	}
}