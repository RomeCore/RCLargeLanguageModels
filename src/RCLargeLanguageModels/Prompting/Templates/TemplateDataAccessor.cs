using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using RCLargeLanguageModels.Prompting.Templates.DataAccessors;
using RCLargeLanguageModels.Prompting.Templates.ExpressionNodes;

namespace RCLargeLanguageModels.Prompting.Templates
{
	/// <summary>
	/// Abstract base class for accessing template data for rendering templates.
	/// </summary>
	public abstract class TemplateDataAccessor : IDisposable
	{
		/// <summary>
		/// Gets the length of the data if it is an array or a string.
		/// </summary>
		public virtual int Length => 0;

		/// <summary>
		/// Gets the template data property associated with the specified property name.
		/// </summary>
		/// <param name="name">The property name to retrieve the template data for.</param>
		/// <returns>The template data that is associated with the specified property name.</returns>
		public virtual TemplateDataAccessor Property(string name) =>
			throw new TemplateRuntimeException($"Cannot get value for key '{name}'");

		/// <summary>
		/// Gets the template data associated with the specified index.
		/// </summary>
		/// <param name="index">The index to retrieve the template data for.</param>
		/// <returns>The template data that is at the specified index.</returns>
		public virtual TemplateDataAccessor Index(TemplateDataAccessor index) =>
			throw new TemplateRuntimeException("Indexing not supported.");

		/// <summary>
		/// Calls a method or function on the current data.
		/// </summary>
		/// <param name="methodName">The name of the method to call.</param>
		/// <param name="arguments">The arguments to pass to the method.</param>
		/// <returns>The result of the method call.</returns>
		public virtual TemplateDataAccessor Call(string methodName, TemplateDataAccessor[] arguments)
		{
			throw new TemplateRuntimeException(
				$"Method '{methodName}' is not supported on type '{GetType().Name}'",
				dataAccessor: this);
		}

		/// <summary>
		/// Gets the value of the template data with the applied unary operator.
		/// </summary>
		/// <param name="type">The unary operator to apply.</param>
		/// <returns>The value of the template data with the applied unary operator, or <see cref="TemplateNullAccessor.Instance"/> if no data is found.</returns>
		public virtual TemplateDataAccessor Operator(UnaryOperatorType type)
		{
			switch (type)
			{
				case UnaryOperatorType.Negate:
					throw new TemplateRuntimeException("Can't apply negate operator to non-numeric data", dataAccessor: this);

				case UnaryOperatorType.LogicalNot:
					return new TemplateBooleanAccessor(!AsBoolean());

				default:
					throw new TemplateRuntimeException("Invalid operator type.", dataAccessor: this);
			}
		}

		/// <summary>
		/// Gets the value of the template data with the applied binary operator.
		/// </summary>
		/// <param name="other">The other accessor to use for the operator.</param>
		/// <param name="type">The binary operator to apply.</param>
		/// <returns>The value of the template data with the applied binary operator, or <see cref="TemplateNullAccessor.Instance"/> if no data is found.</returns>
		public virtual TemplateDataAccessor Operator(TemplateDataAccessor other, BinaryOperatorType type)
		{
			switch (type)
			{
				case BinaryOperatorType.Add:
				case BinaryOperatorType.Subtract:
				case BinaryOperatorType.Multiply:
				case BinaryOperatorType.Divide:
				case BinaryOperatorType.Modulus:
				case BinaryOperatorType.LessThan:
				case BinaryOperatorType.LessThanOrEqual:
				case BinaryOperatorType.GreaterThan:
				case BinaryOperatorType.GreaterThanOrEqual:
					throw new TemplateRuntimeException($"Can't apply binary operator: '{type}' to non-numeric data", dataAccessor: this);

				case BinaryOperatorType.Equal:
					return new TemplateBooleanAccessor(Equals(GetValue(), other.GetValue()));

				case BinaryOperatorType.NotEqual:
					return new TemplateBooleanAccessor(!Equals(GetValue(), other.GetValue()));

				case BinaryOperatorType.LogicalAnd:
					return new TemplateBooleanAccessor(AsBoolean() && other.AsBoolean());

				case BinaryOperatorType.LogicalOr:
					return new TemplateBooleanAccessor(AsBoolean() || other.AsBoolean());

				default:
					throw new TemplateRuntimeException("Invalid operator type.", dataAccessor: this);
			}
		}

		/// <summary>
		/// Gets the value of the current context as a boolean value.
		/// </summary>
		/// <returns>The value of the current context as a boolean value, or <see langword="false"/> if no data is found.</returns>
		public abstract bool AsBoolean();

		/// <summary>
		/// Gets the value of the current context.
		/// </summary>
		/// <returns>The value of the current context.</returns>
		public abstract object GetValue();

		/// <summary>
		/// Converts the template data to a string representation.
		/// </summary>
		/// <param name="format">The format to use for converting the template data, or <see langword="null"/> to use the default format.</param>
		/// <returns>A string representing the template data.</returns>
		public abstract string ToString(string? format = null);

		private bool isDisposed;
		protected virtual void Dispose(bool disposing)
		{
		}

		/// <summary>
		/// Converts the template data to an expression node.
		/// </summary>
		/// <returns>An expression node representing the template data.</returns>
		public TemplateExpressionNode AsExpression()
		{
			return new TemplateDataAccessorExpressionNode(this);
		}

		~TemplateDataAccessor()
		{
			if (!isDisposed)
			{
				Dispose(disposing: false);
				isDisposed = true;
			}
		}

		public void Dispose()
		{
			if (!isDisposed)
			{
				Dispose(disposing: true);
				GC.SuppressFinalize(this);
			}
		}

		/// <summary>
		/// Creates a new template data accessor based on the provided object.
		/// </summary>
		/// <param name="value">The object to create a template data accessor for.</param>
		/// <param name="options">The options to use when creating the template data accessor.</param>
		/// <returns>A new instance of <see cref="TemplateDataAccessor"/>.</returns>
		public static TemplateDataAccessor Create(object? value, DataAccessorCreationOptions options = DataAccessorCreationOptions.None)
		{
			return DataAccessorFactory.Create(value, options);
		}
	}
}