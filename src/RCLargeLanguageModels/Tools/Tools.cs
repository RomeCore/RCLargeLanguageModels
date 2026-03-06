namespace RCLargeLanguageModels.Tools
{
	// Достаем инструменты
	// ☭☭☭☭☭☭

	/// <summary>
	/// Marker interface for AI-executable tools.
	/// </summary>
	/// <remarks>
	/// Implemented by tools that can be invoked through LLM function calling. <para/>
	/// See also: <br/>
	/// <seealso cref="FunctionTool"/>.
	/// </remarks>
	public interface ITool
	{
		/// <summary>
		/// Unique tool identifier
		/// </summary>
		string Name { get; }
	}
}