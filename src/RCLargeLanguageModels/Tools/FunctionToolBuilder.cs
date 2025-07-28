using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace RCLargeLanguageModels.Tools
{
	// Снизу жопа

	internal class FunctionToolParameterBuilder
	{
		public string name = null;
		public bool required = false;
		public JSchema schema = null;
	}

	/// <summary>
	/// Builds a function tool.
	/// </summary>
	public class FunctionToolBuilder
	{
		internal string _name = null;
		internal string _description = null;

		/// <summary>
		/// Creates a new instance of <see cref="FunctionToolBuilder"/> class.
		/// </summary>
		/// <param name="name">The name of function tool.</param>
		public FunctionToolBuilder(string name)
		{
			_name = name ?? throw new ArgumentNullException(nameof(name));
		}

		/// <summary>
		/// Sets the description of the function tool.
		/// </summary>
		/// <param name="description">The description of the function tool.</param>
		public FunctionToolBuilder Description(string description)
		{
			if (_description != null)
				throw new InvalidOperationException("Function description is already set.");

			_description = description ?? throw new ArgumentNullException(nameof(description));

			return this;
		}

		/// <summary>
		/// Adds a first parameter to the function tool.
		/// </summary>
		/// <param name="name">The name of the first parameter.</param>
		public FunctionToolBuilder<T> Parameter<T>(string name)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			return new FunctionToolBuilder<T>(this, new FunctionToolParameterBuilder { name = name });
		}

		/// <summary>
		/// Builds the parameterless function tool.
		/// </summary>
		/// <param name="executor">The executor of the function tool.</param>
		/// <returns>The built function tool.</returns>
		public FunctionTool Build(Func<Task<ToolResult>> executor)
		{
			if (executor == null)
				throw new ArgumentNullException(nameof(executor));

			var schema = new JSchema
			{
				Type = JSchemaType.Object
			};
			schema.Properties.Clear(); // This sets Properties to empty state

			return new FunctionTool(
				_name,
				_description ?? string.Empty,
				schema,
				(t, ct) => executor.Invoke());
		}

		/// <summary>
		/// Builds the parameterless function tool.
		/// </summary>
		/// <param name="executor">The executor of the function tool.</param>
		/// <returns>The built function tool.</returns>
		public FunctionTool Build(Func<CancellationToken, Task<ToolResult>> executor)
		{
			if (executor == null)
				throw new ArgumentNullException(nameof(executor));

			var schema = new JSchema
			{
				Type = JSchemaType.Object
			};
			schema.Properties.Clear(); // This sets Properties to empty state

			return new FunctionTool(
				_name,
				_description ?? string.Empty,
				schema,
				(t, ct) => executor.Invoke(ct));
		}
	}

	public class FunctionToolBuilder<T1> : FunctionToolBuilder
	{
		internal FunctionToolParameterBuilder _parameter1 = new FunctionToolParameterBuilder();

		internal FunctionToolBuilder(
			FunctionToolBuilder source,
			FunctionToolParameterBuilder param1
		) : base(source._name)
		{
			_description = source._description;
			_parameter1 = param1;
		}

		/// <summary>
		/// Sets the description of the first parameter.
		/// </summary>
		/// <param name="description">The description of first parameter.</param>
		public new FunctionToolBuilder<T1> Description(string description)
		{
			if (_parameter1.schema.Description != null)
				throw new InvalidOperationException("First parameter description is already set.");

			_parameter1.schema.Description = description ?? throw new ArgumentNullException(nameof(description));

			return this;
		}

		/// <summary>
		/// Sets the first parameter as required.
		/// </summary>
		public FunctionToolBuilder<T1> Required()
		{
			if (_parameter1.required)
				throw new InvalidOperationException("First parameter is already required.");

			_parameter1.required = true;

			return this;
		}

		/// <summary>
		/// Adds a second parameter to the function tool.
		/// </summary>
		/// <param name="name">The name of the second parameter.</param>
		public new FunctionToolBuilder<T1, T2> Parameter<T2>(string name)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			return new FunctionToolBuilder<T1, T2>(this, new FunctionToolParameterBuilder
				{ name = name, schema = Json.JsonSchemaGenerator.GenerateJSchema(typeof(T2)) });
		}

		/// <summary>
		/// Builds the function tool with one parameter.
		/// </summary>
		/// <param name="executor">The executor of the function tool.</param>
		/// <returns>The built function tool.</returns>
		public FunctionTool Build(Func<T1, Task<ToolResult>> executor)
		{
			if (executor == null)
				throw new ArgumentNullException(nameof(executor));

			var schema = new JSchema
			{
				Type = JSchemaType.Object
			};

			schema.Properties.Add(_parameter1.name, _parameter1.schema);
			if (_parameter1.required)
				schema.Required.Add(_parameter1.name);

			return new FunctionTool(
				_name,
				_description ?? string.Empty,
				schema,
				(t, ct) =>
				{
					var args = (JObject)t;
					var arg1 = args[_parameter1.name].ToObject<T1>();
					return executor.Invoke(arg1);
				});
		}

		/// <summary>
		/// Builds the function tool with one parameter.
		/// </summary>
		/// <param name="executor">The executor of the function tool.</param>
		/// <returns>The built function tool.</returns>
		public FunctionTool Build(Func<T1, CancellationToken, Task<ToolResult>> executor)
		{
			if (executor == null)
				throw new ArgumentNullException(nameof(executor));

			var schema = new JSchema
			{
				Type = JSchemaType.Object
			};

			schema.Properties.Add(_parameter1.name, _parameter1.schema);
			if (_parameter1.required)
				schema.Required.Add(_parameter1.name);

			return new FunctionTool(
				_name,
				_description ?? string.Empty,
				schema,
				(t, ct) =>
				{
					var args = (JObject)t;
					var arg1 = args[_parameter1.name].ToObject<T1>();
					return executor.Invoke(arg1, ct);
				});
		}
	}

	public class FunctionToolBuilder<T1, T2> : FunctionToolBuilder<T1>
	{
		internal FunctionToolParameterBuilder _parameter2;

		internal FunctionToolBuilder(
			FunctionToolBuilder<T1> source,
			FunctionToolParameterBuilder param2
		) : base(source, source._parameter1)
		{
			_parameter2 = param2;
		}

		/// <summary>
		/// Sets the description of the second parameter.
		/// </summary>
		/// <param name="description">The description of second parameter.</param>
		public new FunctionToolBuilder<T1, T2> Description(string description)
		{
			if (_parameter2.schema.Description != null)
				throw new InvalidOperationException("Second parameter description is already set.");

			_parameter2.schema.Description = description ?? throw new ArgumentNullException(nameof(description));

			return this;
		}

		/// <summary>
		/// Sets the second parameter as required.
		/// </summary>
		public new FunctionToolBuilder<T1, T2> Required()
		{
			if (_parameter2.required)
				throw new InvalidOperationException("Second parameter is already required.");

			_parameter2.required = true;

			return this;
		}

		/// <summary>
		/// Adds a third parameter to the function tool.
		/// </summary>
		/// <param name="name">The name of the third parameter.</param>
		public new FunctionToolBuilder<T1, T2, T3> Parameter<T3>(string name)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			return new FunctionToolBuilder<T1, T2, T3>(this, new FunctionToolParameterBuilder
				{ name = name, schema = Json.JsonSchemaGenerator.GenerateJSchema(typeof(T3)) });
		}

		/// <summary>
		/// Builds the function tool with two parameters.
		/// </summary>
		/// <param name="executor">The executor of the function tool.</param>
		/// <returns>The built function tool.</returns>
		public FunctionTool Build(Func<T1, T2, Task<ToolResult>> executor)
		{
			if (executor == null)
				throw new ArgumentNullException(nameof(executor));

			var schema = new JSchema
			{
				Type = JSchemaType.Object
			};

			schema.Properties.Add(_parameter1.name, _parameter1.schema);
			if (_parameter1.required)
				schema.Required.Add(_parameter1.name);

			schema.Properties.Add(_parameter2.name, _parameter2.schema);
			if (_parameter2.required)
				schema.Required.Add(_parameter2.name);

			return new FunctionTool(
				_name,
				_description ?? string.Empty,
				schema,
				(t, ct) =>
				{
					var args = (JObject)t;
					var arg1 = args[_parameter1.name].ToObject<T1>();
					var arg2 = args[_parameter2.name].ToObject<T2>();
					return executor.Invoke(arg1, arg2);
				});
		}

		/// <summary>
		/// Builds the function tool with two parameters.
		/// </summary>
		/// <param name="executor">The executor of the function tool.</param>
		/// <returns>The built function tool.</returns>
		public FunctionTool Build(Func<T1, T2, CancellationToken, Task<ToolResult>> executor)
		{
			if (executor == null)
				throw new ArgumentNullException(nameof(executor));

			var schema = new JSchema
			{
				Type = JSchemaType.Object
			};

			schema.Properties.Add(_parameter1.name, _parameter1.schema);
			if (_parameter1.required)
				schema.Required.Add(_parameter1.name);

			schema.Properties.Add(_parameter2.name, _parameter2.schema);
			if (_parameter2.required)
				schema.Required.Add(_parameter2.name);

			return new FunctionTool(
				_name,
				_description ?? string.Empty,
				schema,
				(t, ct) =>
				{
					var args = (JObject)t;
					var arg1 = args[_parameter1.name].ToObject<T1>();
					var arg2 = args[_parameter2.name].ToObject<T2>();
					return executor.Invoke(arg1, arg2, ct);
				});
		}
	}

	public class FunctionToolBuilder<T1, T2, T3> : FunctionToolBuilder<T1, T2>
	{
		internal FunctionToolParameterBuilder _parameter3;

		internal FunctionToolBuilder(
			FunctionToolBuilder<T1, T2> source,
			FunctionToolParameterBuilder param3
		) : base(source, source._parameter2)
		{
			_parameter3 = param3;
		}

		/// <summary>
		/// Sets the description of the third parameter.
		/// </summary>
		/// <param name="description">The description of third parameter.</param>
		public new FunctionToolBuilder<T1, T2, T3> Description(string description)
		{
			if (_parameter3.schema.Description != null)
				throw new InvalidOperationException("Third parameter description is already set.");

			_parameter3.schema.Description = description ?? throw new ArgumentNullException(nameof(description));

			return this;
		}

		/// <summary>
		/// Sets the third parameter as required.
		/// </summary>
		public new FunctionToolBuilder<T1, T2, T3> Required()
		{
			if (_parameter3.required)
				throw new InvalidOperationException("Third parameter is already required.");

			_parameter3.required = true;

			return this;
		}

		/// <summary>
		/// Adds a fourth parameter to the function tool.
		/// </summary>
		/// <param name="name">The name of the fourth parameter.</param>
		public new FunctionToolBuilder<T1, T2, T3, T4> Parameter<T4>(string name)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			return new FunctionToolBuilder<T1, T2, T3, T4>(this, new FunctionToolParameterBuilder
				{ name = name, schema = Json.JsonSchemaGenerator.GenerateJSchema(typeof(T4)) });
		}

		/// <summary>
		/// Builds the function tool with three parameters.
		/// </summary>
		/// <param name="executor">The executor of the function tool.</param>
		/// <returns>The built function tool.</returns>
		public FunctionTool Build(Func<T1, T2, T3, Task<ToolResult>> executor)
		{
			if (executor == null)
				throw new ArgumentNullException(nameof(executor));

			var schema = new JSchema
			{
				Type = JSchemaType.Object
			};

			schema.Properties.Add(_parameter1.name, _parameter1.schema);
			if (_parameter1.required)
				schema.Required.Add(_parameter1.name);

			schema.Properties.Add(_parameter2.name, _parameter2.schema);
			if (_parameter2.required)
				schema.Required.Add(_parameter2.name);

			schema.Properties.Add(_parameter3.name, _parameter3.schema);
			if (_parameter3.required)
				schema.Required.Add(_parameter3.name);

			return new FunctionTool(
				_name,
				_description ?? string.Empty,
				schema,
				(t, ct) =>
				{
					var args = (JObject)t;
					var arg1 = args[_parameter1.name].ToObject<T1>();
					var arg2 = args[_parameter2.name].ToObject<T2>();
					var arg3 = args[_parameter3.name].ToObject<T3>();
					return executor.Invoke(arg1, arg2, arg3);
				});
		}

		/// <summary>
		/// Builds the function tool with three parameters.
		/// </summary>
		/// <param name="executor">The executor of the function tool.</param>
		/// <returns>The built function tool.</returns>
		public FunctionTool Build(Func<T1, T2, T3, CancellationToken, Task<ToolResult>> executor)
		{
			if (executor == null)
				throw new ArgumentNullException(nameof(executor));

			var schema = new JSchema
			{
				Type = JSchemaType.Object
			};

			schema.Properties.Add(_parameter1.name, _parameter1.schema);
			if (_parameter1.required)
				schema.Required.Add(_parameter1.name);

			schema.Properties.Add(_parameter2.name, _parameter2.schema);
			if (_parameter2.required)
				schema.Required.Add(_parameter2.name);

			schema.Properties.Add(_parameter3.name, _parameter3.schema);
			if (_parameter3.required)
				schema.Required.Add(_parameter3.name);

			return new FunctionTool(
				_name,
				_description ?? string.Empty,
				schema,
				(t, ct) =>
				{
					var args = (JObject)t;
					var arg1 = args[_parameter1.name].ToObject<T1>();
					var arg2 = args[_parameter2.name].ToObject<T2>();
					var arg3 = args[_parameter3.name].ToObject<T3>();
					return executor.Invoke(arg1, arg2, arg3, ct);
				});
		}
	}

	public class FunctionToolBuilder<T1, T2, T3, T4> : FunctionToolBuilder<T1, T2, T3>
	{
		internal FunctionToolParameterBuilder _parameter4;

		internal FunctionToolBuilder(
			FunctionToolBuilder<T1, T2, T3> source,
			FunctionToolParameterBuilder param4
		) : base(source, source._parameter3)
		{
			_parameter4 = param4;
		}

		/// <summary>
		/// Sets the description of the fourth parameter.
		/// </summary>
		/// <param name="description">The description of fourth parameter.</param>
		public new FunctionToolBuilder<T1, T2, T3, T4> Description(string description)
		{
			if (_parameter4.schema.Description != null)
				throw new InvalidOperationException("Fourth parameter description is already set.");

			_parameter4.schema.Description = description ?? throw new ArgumentNullException(nameof(description));

			return this;
		}

		/// <summary>
		/// Sets the fourth parameter as required.
		/// </summary>
		public new FunctionToolBuilder<T1, T2, T3, T4> Required()
		{
			if (_parameter4.required)
				throw new InvalidOperationException("Fourth parameter is already required.");

			_parameter4.required = true;

			return this;
		}

		/// <summary>
		/// Builds the function tool with four parameters.
		/// </summary>
		/// <param name="executor">The executor of the function tool.</param>
		/// <returns>The built function tool.</returns>
		public FunctionTool Build(Func<T1, T2, T3, T4, Task<ToolResult>> executor)
		{
			if (executor == null)
				throw new ArgumentNullException(nameof(executor));

			var schema = new JSchema
			{
				Type = JSchemaType.Object
			};

			schema.Properties.Add(_parameter1.name, _parameter1.schema);
			if (_parameter1.required)
				schema.Required.Add(_parameter1.name);

			schema.Properties.Add(_parameter2.name, _parameter2.schema);
			if (_parameter2.required)
				schema.Required.Add(_parameter2.name);

			schema.Properties.Add(_parameter3.name, _parameter3.schema);
			if (_parameter3.required)
				schema.Required.Add(_parameter3.name);

			schema.Properties.Add(_parameter4.name, _parameter4.schema);
			if (_parameter4.required)
				schema.Required.Add(_parameter4.name);

			return new FunctionTool(
				_name,
				_description ?? string.Empty,
				schema,
				(t, ct) =>
				{
					var args = (JObject)t;
					var arg1 = args[_parameter1.name].ToObject<T1>();
					var arg2 = args[_parameter2.name].ToObject<T2>();
					var arg3 = args[_parameter3.name].ToObject<T3>();
					var arg4 = args[_parameter4.name].ToObject<T4>();
					return executor.Invoke(arg1, arg2, arg3, arg4);
				});
		}

		/// <summary>
		/// Builds the function tool with four parameters.
		/// </summary>
		/// <param name="executor">The executor of the function tool.</param>
		/// <returns>The built function tool.</returns>
		public FunctionTool Build(Func<T1, T2, T3, T4, CancellationToken, Task<ToolResult>> executor)
		{
			if (executor == null)
				throw new ArgumentNullException(nameof(executor));

			var schema = new JSchema
			{
				Type = JSchemaType.Object
			};

			schema.Properties.Add(_parameter1.name, _parameter1.schema);
			if (_parameter1.required)
				schema.Required.Add(_parameter1.name);

			schema.Properties.Add(_parameter2.name, _parameter2.schema);
			if (_parameter2.required)
				schema.Required.Add(_parameter2.name);

			schema.Properties.Add(_parameter3.name, _parameter3.schema);
			if (_parameter3.required)
				schema.Required.Add(_parameter3.name);

			schema.Properties.Add(_parameter4.name, _parameter4.schema);
			if (_parameter4.required)
				schema.Required.Add(_parameter4.name);

			return new FunctionTool(
				_name,
				_description ?? string.Empty,
				schema,
				(t, ct) =>
				{
					var args = (JObject)t;
					var arg1 = args[_parameter1.name].ToObject<T1>();
					var arg2 = args[_parameter2.name].ToObject<T2>();
					var arg3 = args[_parameter3.name].ToObject<T3>();
					var arg4 = args[_parameter4.name].ToObject<T4>();
					return executor.Invoke(arg1, arg2, arg3, arg4, ct);
				});
		}
	}
}