using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace RCLargeLanguageModels.Prompting.Templates
{
	/// <summary>
	/// A set of functions that can be called inside a template.
	/// </summary>
	public class TemplateFunctionSet
	{
		private ImmutableDictionary<string, TemplateFunction> _functions;

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateFunctionSet"/> class.
		/// </summary>
		/// <param name="functions">A collection of template functions. Functions must have unique not-<see langword="null"/> names.</param>
		/// <exception cref="ArgumentNullException">Thrown when the <paramref name="functions"/> parameter is null.</exception>
		public TemplateFunctionSet(IEnumerable<TemplateFunction> functions)
		{
			_functions = functions?.ToImmutableDictionary(k => k.Name, v => v) ?? throw new ArgumentNullException(nameof(functions));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateFunctionSet"/> class.
		/// </summary>
		/// <param name="functions">A collection of template functions. Functions must have unique not-<see langword="null"/> names.</param>
		/// <exception cref="ArgumentNullException">Thrown when the <paramref name="functions"/> parameter is null.</exception>
		public TemplateFunctionSet(params TemplateFunction[] functions)
		{
			_functions = functions?.ToImmutableDictionary(k => k.Name, v => v) ?? throw new ArgumentNullException(nameof(functions));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateFunctionSet"/> class.
		/// </summary>
		/// <param name="functions">A dictionary of function names and their corresponding implementations.</param>
		/// <exception cref="ArgumentNullException">Thrown when the <paramref name="functions"/> parameter is null.</exception>
		public TemplateFunctionSet(IDictionary<string, TemplateFunction> functions)
		{
			_functions = functions?.ToImmutableDictionary() ?? throw new ArgumentNullException(nameof(functions));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateFunctionSet"/> class.
		/// </summary>
		/// <param name="functions">A dictionary of function names and their corresponding implementations.</param>
		/// <exception cref="ArgumentNullException">Thrown when the <paramref name="functions"/> parameter is null.</exception>
		public TemplateFunctionSet(ImmutableDictionary<string, TemplateFunction> functions)
		{
			_functions = functions ?? throw new ArgumentNullException(nameof(functions));
		}

		/// <summary>
		/// Gets the function with the specified name.
		/// </summary>
		/// <param name="functionName">The name of the function.</param>
		/// <returns>The function with the specified name, or <see langword="null"/> if no such function exists.</returns>
		public TemplateFunction? TryGetFunction(string functionName)
		{
			if (_functions.TryGetValue(functionName, out var function))
				return function;
			return null;
		}

		/// <summary>
		/// Gets the function with the specified name. Throws an exception if no such function exists.
		/// </summary>
		/// <param name="functionName">The name of the function.</param>
		/// <returns>The function with the specified name.</returns>
		public TemplateFunction GetFunction(string functionName)
		{
			if (_functions.TryGetValue(functionName, out var function))
				return function;
			throw new KeyNotFoundException($"No function named '{functionName}' found in the set.");
		}

		/// <summary>
		/// Executes a function with the specified name and arguments.
		/// </summary>
		/// <param name="functionName">The name of the function.</param>
		/// <param name="args">The arguments to pass to the function.</param>
		/// <returns>The result of executing the function.</returns>
		public TemplateDataAccessor CallFunction(string functionName, TemplateDataAccessor[] args)
		{
			var function = GetFunction(functionName);
			return function.Call(null, args);
		}

		/// <summary>
		/// Executes a function with the specified name, context and arguments.
		/// </summary>
		/// <param name="functionName">The name of the function.</param>
		/// <param name="self">The data accessor representing the current context.</param>
		/// <param name="args">The arguments to pass to the function.</param>
		/// <returns>The result of executing the function.</returns>
		public TemplateDataAccessor CallFunction(string functionName, TemplateDataAccessor? self, TemplateDataAccessor[] args)
		{
			var function = GetFunction(functionName);
			return function.Call(self, args);
		}

		/// <summary>
		/// Gets the default set of functions.
		/// </summary>
		public static TemplateFunctionSet Default { get; }

		static TemplateFunctionSet()
		{
			Default = new TemplateFunctionSet(
				new TemplateFunction("length", (self, args) => self.Length),
				new TemplateFunction("strcat", (self, args) => string.Join("", args.Select(a => a.GetValue().ToString()))),
				new TemplateFunction("substr", (self, args) => args[0].GetValue().ToString().Substring((int)args[1].GetValue(), (int)args[2].GetValue()))
			);
		}
	}
}