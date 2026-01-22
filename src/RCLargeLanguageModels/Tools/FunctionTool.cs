using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Serilog;

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
		private readonly Func<JToken, CancellationToken, Task<ToolResult>> _function;

		public string Name { get; }
		public string Description { get; }
		public JSchema ArgumentSchema { get; }

		/// <summary>
		/// Initializes a new AI-callable function.
		/// </summary>
		/// <param name="name">Tool identifier.</param>
		/// <param name="description">LLM-readable description.</param>
		/// <param name="argumentSchema">Input argument JSON schema.</param>
		/// <param name="function">Execution implementation.</param>
		public FunctionTool(string name, string description, JSchema argumentSchema, Func<JToken, CancellationToken, Task<ToolResult>> function)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Description = description ?? throw new ArgumentNullException(nameof(description));
			ArgumentSchema = argumentSchema ?? throw new ArgumentNullException(nameof(argumentSchema));
			_function = function ?? throw new ArgumentNullException(nameof(function));
		}

		public async Task<ToolResult> ExecuteAsync(JToken args, CancellationToken cancellationToken = default)
		{
			if (args == null)
				throw new ArgumentNullException(nameof(args));
			if (!args.IsValid(ArgumentSchema))
				throw new ArgumentException($"Invalid JSON argument (not compatible to schema): {args}");

			return await _function.Invoke(args, cancellationToken);
		}


		/// <summary>
		/// Creates a function tool from a delegate.
		/// </summary>
		/// <param name="delegate">The delegate to wrap.</param>
		/// <param name="name">Tool identifier.</param>
		/// <param name="description">LLM-readable description.</param>
		/// <returns>The function tool.</returns>
		/// <exception cref="ArgumentNullException"/>
		public static FunctionTool From(Delegate @delegate, string name = null, string description = null)
		{
			if (@delegate == null)
				throw new ArgumentNullException(nameof(@delegate));

			return From(@delegate.Target, @delegate.Method, name, description);
		}

		/// <summary>
		/// Creates a function tool from a static method.
		/// </summary>
		/// <param name="method">The static method to wrap.</param>
		/// <param name="name">Tool identifier.</param>
		/// <param name="description">LLM-readable description.</param>
		/// <returns>The function tool.</returns>
		/// <exception cref="ArgumentNullException"/>
		public static FunctionTool From(MethodInfo method, string name = null, string description = null)
		{
			return From(null, method, name, description);
		}

		/// <summary>
		/// Creates a function tool from a method and its target.
		/// </summary>
		/// <param name="methodTarget">The method target, if null, method must be static.</param>
		/// <param name="method">The method to wrap.</param>
		/// <param name="name">Tool identifier.</param>
		/// <param name="description">LLM-readable description.</param>
		/// <returns>The function tool.</returns>
		/// <exception cref="ArgumentNullException"/>
		public static FunctionTool From(object methodTarget, MethodInfo method, string name = null, string description = null)
		{
			if (method == null)
				throw new ArgumentNullException(nameof(method));
			if (methodTarget == null && !method.IsStatic)
				throw new ArgumentException("If method target is null, method must be static.", nameof(method));

			name = name ?? method.Name;

			var ret = method.ReturnType;
			if (
				ret != typeof(ToolResult) &&
				ret != typeof(Task<ToolResult>) &&
				ret != typeof(string) &&
				ret != typeof(Task<string>) &&
				ret != typeof(Task)
				)
				throw new ArgumentException("Return type must be ToolResult, Task<ToolResult>, string, Task<string> or Task.", nameof(method));

			var parameters = method.GetParameters();
			var schema = Json.JsonSchemaGenerator.GenerateJSchema(method);
			var mappings = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			int ctMapping = -1;

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
					var argName = param.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? param.Name;

					mappings.Add(argName, param.Position);
				}
			}

			async Task<ToolResult> Func(JToken args, CancellationToken cancellationToken)
			{
				var obj = args as JObject;
				var inParams = new object[parameters.Length];

				for (int i = 0; i < parameters.Length; i++)
					if (parameters[i].HasDefaultValue)
						inParams[i] = parameters[i].DefaultValue;

				foreach (var kvp in mappings)
				{
					var arg = obj[kvp.Key];
					if (arg == null)
						continue;

					var type = parameters[kvp.Value].ParameterType;
					inParams[kvp.Value] = arg.ToObject(type);
				}

				if (ctMapping != -1)
					inParams[ctMapping] = cancellationToken;

				try
				{
					object value;

					if (ctMapping == -1)
					{
						if (ret == typeof(Task<ToolResult>))
						{
							value = Task.Run(() =>
							{
								var _value = (Task<ToolResult>)method.Invoke(methodTarget, inParams);
								return _value;
							}, cancellationToken);
						}
						else if (ret == typeof(Task<string>))
						{
							value = Task.Run(() =>
							{
								var _value = (Task<string>)method.Invoke(methodTarget, inParams);
								return _value;
							}, cancellationToken);
						}
						else if (ret == typeof(Task))
						{
							value = Task.Run(() =>
							{
								var _value = (Task)method.Invoke(methodTarget, inParams);
								return _value;
							}, cancellationToken);
						}
						else
							value = method.Invoke(methodTarget, inParams);
					}
					else
						value = method.Invoke(methodTarget, inParams);

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
							return new ToolResult("SUCCESS");

						default:
							throw new InvalidOperationException($"Invalid return type: {value.GetType()}");
					}
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Error executing function {Name}", name);
					return new ToolResult("FAIL");
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
		public FunctionTool(string name, string description, Func<TArg, CancellationToken, Task<ToolResult>> function) :
			base(name, description, GetSchema(), WrapFunction(function))
		{
		}
		
		/// <summary>
		/// Initializes a new AI-callable function.
		/// </summary>
		/// <param name="name">Tool identifier.</param>
		/// <param name="description">LLM-readable description.</param>
		/// <param name="function">Execution implementation.</param>
		public FunctionTool(string name, string description, Func<TArg, Task<ToolResult>> function) :
			base(name, description, GetSchema(), WrapFunction(function))
		{
		}

		private static JSchema GetSchema()
		{
			return Json.JsonSchemaGenerator.GenerateJSchema(typeof(TArg));
		}

		private static Func<JToken, CancellationToken, Task<ToolResult>> WrapFunction(
			Func<TArg, CancellationToken, Task<ToolResult>> function)
		{
			return async (jObj, ct) =>
			{
				var obj = jObj.ToObject<TArg>();
				return await function.Invoke(obj, ct);
			};
		}

		private static Func<JToken, CancellationToken, Task<ToolResult>> WrapFunction(
			Func<TArg, Task<ToolResult>> function)
		{
			return async (jObj, ct) =>
			{
				var obj = jObj.ToObject<TArg>();
				return await function.Invoke(obj);
			};
		}
	}
}