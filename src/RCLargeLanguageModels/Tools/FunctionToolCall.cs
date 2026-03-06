using System;
using System.Text.Json.Nodes;

namespace RCLargeLanguageModels.Tools
{
	/// <summary>
	/// Represents a LLM function tool call.
	/// </summary>
	public class FunctionToolCall : IToolCall
	{
		/// <inheritdoc/>
		public string Id { get; }

		/// <inheritdoc/>
		public string ToolName { get; }

		/// <summary>
		/// The object that contains arguments of the function tool call.
		/// </summary>
		public JsonNode Args { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="FunctionToolCall"/> class with auto-generated identifier.
		/// </summary>
		/// <param name="toolName">The original function tool name that been called.</param>
		/// <param name="args">The args object of the function tool call.</param>
		public FunctionToolCall(string toolName, JsonNode args)
		{
			Id = $"call_0_{Guid.NewGuid()}";
			ToolName = toolName ?? throw new ArgumentNullException(nameof(toolName));
			Args = args ?? throw new ArgumentNullException(nameof(args));
		}

		/// <summary>
		/// Creates a new instance of the <see cref="FunctionToolCall"/> class.
		/// </summary>
		/// <param name="id">The tool call identifier.</param>
		/// <param name="toolName">The original function tool name that been called.</param>
		/// <param name="args">The args object of the function tool call.</param>
		public FunctionToolCall(string id, string toolName, JsonNode args)
		{
			Id = id ?? throw new ArgumentNullException(nameof(id));
			ToolName = toolName ?? throw new ArgumentNullException(nameof(toolName));
			Args = args ?? throw new ArgumentNullException(nameof(args));
		}
	}
}