using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

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

	/// <summary>
	/// Represents a function tool that can be invoked by an AI model.
	/// </summary>
	public interface IFunctionTool : ITool
	{
		/// <summary>
		/// Natural language description of tool purpose.
		/// </summary>
		/// <remarks>
		/// Used by LLMs to determine appropriate tool usage.
		/// </remarks>
		string Description { get; }

		/// <summary>
		/// JSON schema for the function arguments.
		/// </summary>
		JSchema ArgumentSchema { get; }

		/// <summary>
		/// Invokes the tool with the provided arguments.
		/// </summary>
		/// <param name="args">Function arguments in JSON format.</param>
		/// <param name="cancellationToken">Cancellation token used to cancel the operation.</param>
		/// <returns>The task representation of tool result.</returns>
		Task<ToolResult> ExecuteAsync(JToken args, CancellationToken cancellationToken = default);
	}
}