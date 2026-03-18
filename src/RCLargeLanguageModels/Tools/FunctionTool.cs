using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Json;
using RCLargeLanguageModels.Json.Schema;

namespace RCLargeLanguageModels.Tools
{
	/// <summary>
	/// Represents an executable tool that can be invoked by an AI model.
	/// </summary>
	/// <remarks>
	/// Implements the function calling pattern for LLMs.
	/// </remarks>
	public class FunctionTool : ITool
	{
		private static readonly JsonSchemaMethodGenerator _schemaGenerator = new();
		private readonly Func<JsonNode, CancellationToken, Task<ToolResult>> _function;

		public string Name { get; }
		public string Description { get; }
		public JsonObject ArgumentSchema { get; }

		/// <summary>
		/// Initializes a new AI-callable function.
		/// </summary>
		/// <param name="name">Tool identifier.</param>
		/// <param name="description">LLM-readable description.</param>
		/// <param name="argumentSchema">Input argument JSON schema.</param>
		/// <param name="function">Execution implementation.</param>
		public FunctionTool(string name, string description, JsonObject argumentSchema,
			Func<JsonNode, CancellationToken, Task<ToolResult>> function)
		{
			ToolName.EnsureValid(name);
			Name = name;
			Description = description ?? throw new ArgumentNullException(nameof(description));
			ArgumentSchema = argumentSchema ?? throw new ArgumentNullException(nameof(argumentSchema));
			_function = function ?? throw new ArgumentNullException(nameof(function));
		}

		/// <summary>
		/// Executes the function with the provided arguments.
		/// </summary>
		/// <param name="args">The arguments to pass to the function.</param>
		/// <param name="cancellationToken">The cancellation token to use for the operation. </param>
		/// <returns>The result of the function execution.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the arguments are null.</exception>
		public Task<ToolResult> ExecuteAsync(JsonNode args, CancellationToken cancellationToken = default)
		{
			if (args == null)
				throw new ArgumentNullException(nameof(args));

			return _function.Invoke(args, cancellationToken);
		}

		/// <summary>
		/// Creates a function tool from a delegate.
		/// </summary>
		/// <param name="delegate">The delegate to wrap.</param>
		/// <param name="name">Tool identifier.</param>
		/// <param name="description">LLM-readable description.</param>
		/// <param name="schemaProperties">Optional properties for the JSON schema generator.</param>
		/// <param name="serializerOptions">Optional JSON serializer options.</param>
		/// <returns>The function tool.</returns>
		/// <exception cref="ArgumentNullException"/>
		public static FunctionTool From(Delegate @delegate, string? name = null, string? description = null,
			JsonSchemaGeneratorProperties? schemaProperties = null, JsonSerializerOptions? serializerOptions = null)
		{
			if (@delegate == null)
				throw new ArgumentNullException(nameof(@delegate));

			return From(@delegate.Target, @delegate.Method, name, description, schemaProperties, serializerOptions);
		}

		/// <summary>
		/// Creates a function tool from a static method.
		/// </summary>
		/// <param name="method">The static method to wrap.</param>
		/// <param name="name">Tool identifier.</param>
		/// <param name="description">LLM-readable description.</param>
		/// <param name="schemaProperties">Optional properties for the JSON schema generator.</param>
		/// <param name="serializerOptions">Optional JSON serializer options.</param>
		/// <returns>The function tool.</returns>
		/// <exception cref="ArgumentNullException"/>
		public static FunctionTool From(MethodInfo method, string? name = null, string? description = null,
			JsonSchemaGeneratorProperties? schemaProperties = null, JsonSerializerOptions? serializerOptions = null)
		{
			return From(null, method, name, description, schemaProperties, serializerOptions);
		}

		/// <summary>
		/// Creates a function tool from a method and its target.
		/// </summary>
		/// <param name="methodTarget">The method target, if null, method must be static.</param>
		/// <param name="method">The method to wrap.</param>
		/// <param name="name">Tool identifier.</param>
		/// <param name="description">LLM-readable description.</param>
		/// <param name="schemaProperties">Optional properties for the JSON schema generator.</param>
		/// <param name="serializerOptions">Optional JSON serializer options.</param>
		/// <returns>The function tool.</returns>
		/// <exception cref="ArgumentNullException"/>
		public static FunctionTool From(object? methodTarget, MethodInfo method, string? name = null, string? description = null,
			JsonSchemaGeneratorProperties? schemaProperties = null, JsonSerializerOptions? serializerOptions = null)
		{
			if (method == null)
				throw new ArgumentNullException(nameof(method));
			if (methodTarget == null && !method.IsStatic)
				throw new ArgumentException("If method target is null, method must be static.", nameof(method));

			var ret = method.ReturnType;
			if (
				ret != typeof(ToolResult) &&
				ret != typeof(Task<ToolResult>) &&
				ret != typeof(string) &&
				ret != typeof(Task<string>) &&
				ret != typeof(Task)
				)
				throw new ArgumentException("Return type must be ToolResult, Task<ToolResult>, string, Task<string> or Task.", nameof(method));

			var methodAccessor = new JsonMemberAccessor(method);
			schemaProperties ??= new JsonSchemaGeneratorProperties();
			var schema = _schemaGenerator.GenerateSchema(methodAccessor, schemaProperties)!;

			var mappings = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			int ctMapping = -1;

			name = name ??
				methodAccessor.Name;
			ToolName.EnsureValid(name);
			description = description ??
				methodAccessor.Attributes.Get<DescriptionAttribute>()?.Description ??
				string.Empty;

			var parameters = method.GetParameters();
			foreach (var param in parameters)
			{
				if (param.ParameterType == typeof(CancellationToken))
				{
					if (ctMapping != -1)
						throw new ArgumentException("Multiple parameters of type CancellationToken are not supported.", nameof(method));
					ctMapping = param.Position;
				}
				else
				{
					var argName = param.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
						?? param.Name!;
					mappings.Add(argName, param.Position);
				}
			}

			async Task<ToolResult> Func(JsonNode args, CancellationToken cancellationToken)
			{
				if (args is not JsonObject obj)
					throw new ArgumentException("Arguments must be a JSON object.");

				var inParams = new object[parameters.Length];

				try
				{
					for (int i = 0; i < parameters.Length; i++)
						if (parameters[i].HasDefaultValue)
							inParams[i] = parameters[i].DefaultValue!;

					foreach (var kvp in mappings)
					{
						var arg = obj[kvp.Key];
						if (arg == null)
							continue;

						var type = parameters[kvp.Value].ParameterType;
						inParams[kvp.Value] = JsonSerializer.Deserialize(arg, type, serializerOptions)!;
					}

					if (ctMapping != -1)
						inParams[ctMapping] = cancellationToken;
				}
				catch (Exception ex)
				{
					throw new ArgumentException("Failed to deserialize arguments.", nameof(args), ex);
				}

				var value = method.Invoke(methodTarget, inParams)!;

				switch (value)
				{
					case Task<ToolResult> _1:
						return await _1;

					case ToolResult _2:
						return _2;

					case Task<string> _3:
						return new ToolResult(await _3);

					case string _4:
						return new ToolResult(_4);

					case Task _5:
						await _5;
						return new ToolResult(ToolResultStatus.Success);

					default:
						// Should be never reach here
						throw new InvalidOperationException($"Invalid return type: {value.GetType()}");
				}
			}

			return new FunctionTool(name, description, schema, Func);
		}
	}

	/// <summary>
	/// Represents an executable function tool that can be invoked by an AI model.
	/// </summary>
	/// <typeparam name="TArg">Type of the function argument, will be converted to JSON schema.</typeparam>
	public class FunctionTool<TArg> : FunctionTool
	{
		/// <summary>
		/// Initializes a new AI-callable function.
		/// </summary>
		/// <param name="name">Tool identifier.</param>
		/// <param name="description">LLM-readable description.</param>
		/// <param name="function">Execution implementation.</param>
		/// <param name="schemaProperties">Optional properties for the JSON schema generator.</param>
		/// <param name="serializerOptions">Optional JSON serializer options.</param>
		public FunctionTool(string name, string description, Func<TArg, CancellationToken, Task<ToolResult>> function,
			JsonSchemaGeneratorProperties? schemaProperties = null, JsonSerializerOptions? serializerOptions = null) :
			base(name, description, GetSchema(schemaProperties), WrapFunction(function, serializerOptions))
		{
		}

		/// <summary>
		/// Initializes a new AI-callable function.
		/// </summary>
		/// <param name="name">Tool identifier.</param>
		/// <param name="description">LLM-readable description.</param>
		/// <param name="function">Execution implementation.</param>
		/// <param name="schemaProperties">Optional properties for the JSON schema generator.</param>
		/// <param name="serializerOptions">Optional JSON serializer options.</param>
		public FunctionTool(string name, string description, Func<TArg, Task<ToolResult>> function,
			JsonSchemaGeneratorProperties? schemaProperties = null, JsonSerializerOptions? serializerOptions = null) :
			base(name, description, GetSchema(schemaProperties), WrapFunction(function, serializerOptions))
		{
		}

		private static JsonObject GetSchema(JsonSchemaGeneratorProperties? schemaProperties)
		{
			return JsonSchemaGenerator.Generate(typeof(TArg), schemaProperties);
		}

		private static Func<JsonNode, CancellationToken, Task<ToolResult>> WrapFunction(
			Func<TArg, CancellationToken, Task<ToolResult>> function, JsonSerializerOptions? serializerOptions)
		{
			return async (jObj, ct) =>
			{
				var obj = jObj.Deserialize<TArg>(serializerOptions) ?? throw new JsonException("Failed to deserialize argument.");
				return await function.Invoke(obj, ct);
			};
		}

		private static Func<JsonNode, CancellationToken, Task<ToolResult>> WrapFunction(
			Func<TArg, Task<ToolResult>> function, JsonSerializerOptions? serializerOptions)
		{
			return async (jObj, ct) =>
			{
				var obj = jObj.Deserialize<TArg>(serializerOptions) ?? throw new JsonException("Failed to deserialize argument.");
				return await function.Invoke(obj);
			};
		}
	}
}