using System;

namespace RCLargeLanguageModels.Prompting.Templates
{
	/// <summary>
	/// Represents a function that can be called inside a template.
	/// </summary>
	public class TemplateFunction
	{
		private readonly Func<TemplateDataAccessor[], TemplateDataAccessor> _function;

		/// <summary>
		/// Gets the name of the function.
		/// </summary>
		public string? Name { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateFunction"/> class.
		/// </summary>
		/// <param name="function">The function to be called.</param>
		/// <exception cref="ArgumentNullException"></exception>
		public TemplateFunction(Func<TemplateDataAccessor[], TemplateDataAccessor> function)
		{
			Name = null;
			_function = function ?? throw new ArgumentNullException(nameof(function));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateFunction"/> class.
		/// </summary>
		/// <param name="name">The name of the function.</param>
		/// <param name="function">The function to be called.</param>
		/// <exception cref="ArgumentNullException"></exception>
		public TemplateFunction(string? name, Func<TemplateDataAccessor[], TemplateDataAccessor> function)
		{
			Name = name;
			_function = function ?? throw new ArgumentNullException(nameof(function));
		}

		/// <summary>
		/// Calls the function with the provided parameters.
		/// </summary>
		/// <param name="parameters">The parameters to pass to the function.</param>
		/// <returns>The result of the function call.</returns>
		public TemplateDataAccessor Call(TemplateDataAccessor[] parameters)
		{
			return _function(parameters ?? throw new ArgumentNullException(nameof(parameters)));
		}
	}
}