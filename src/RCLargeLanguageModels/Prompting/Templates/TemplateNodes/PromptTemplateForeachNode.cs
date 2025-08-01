using System;
using System.Collections.Generic;
using System.Text;
using RCLargeLanguageModels.Prompting.Templates.DataAccessors;

namespace RCLargeLanguageModels.Prompting.Templates.TemplateNodes
{
	/// <summary>
	/// Represents a node in the prompt template that iterates over a collection of items.
	/// </summary>
	public class PromptTemplateForeachNode : PromptTemplateNode
	{
		/// <summary>
		/// Gets the source expression that provides the data for iteration.
		/// </summary>
		public TemplateExpressionNode Source { get; }

		/// <summary>
		/// Gets the child node that will be executed for each item in the collection.
		/// </summary>
		public PromptTemplateNode Child { get; }

		/// <summary>
		/// Gets the name of the variable that represents each item in the iteration.
		/// </summary>
		public string IterableName { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="PromptTemplateForeachNode"/> class.
		/// </summary>
		/// <param name="source">The expression that provides the data for iteration.</param>
		/// <param name="child">The node that will be executed for each item in the colletion.</param>
		/// <param name="iterableName">The name of the variable that represents each item in the iteration.</param>
		/// <exception cref="ArgumentNullException">Thrown when the <paramref name="source"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown when the <paramref name="iterableName"/> is null or empty.</exception>
		public PromptTemplateForeachNode(TemplateExpressionNode source, PromptTemplateNode child, string iterableName)
		{
			Source = source ?? throw new ArgumentNullException(nameof(source));
			Child = child ?? throw new ArgumentNullException(nameof(child));
			IterableName = string.IsNullOrEmpty(iterableName) ? throw new ArgumentException("The iterable name cannot be null or empty.", nameof(iterableName)) : iterableName;
		}

		public override string Render(TemplateContextAccessor context)
		{
			var source = Source.Evaluate(context);

			if (source is not IEnumerableTemplateDataAccessor enumerableSource)
				throw new TemplateRuntimeException($"The source expression does not provide an enumerable data source.",
					dataAccessor: context, expressionNode: Source);

			StringBuilder result = new StringBuilder();

			context.PushFrame();
			foreach (var item in enumerableSource)
			{
				context.SetVariable(IterableName, item);

				var childResult = Child.Render(context);
				if (!string.IsNullOrEmpty(childResult))
					result.Append(childResult);
			}
			context.PopFrame();

			return result.ToString();
		}
	}
}