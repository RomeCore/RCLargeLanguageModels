using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Text;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Metadata;
using RCLargeLanguageModels.Prompting.Templates.DataAccessors;

namespace RCLargeLanguageModels.Prompting.Templates
{
	/// <summary>
	/// Represents a frame for managing local template context variables.
	/// </summary>
	public class TemplateContextFrame
	{
		private readonly Dictionary<string, TemplateDataAccessor> _variables;

		/// <summary>
		/// Initializes a new instance of the <see cref="TemplateContextFrame"/> class.
		/// </summary>
		public TemplateContextFrame()
		{
			_variables = new Dictionary<string, TemplateDataAccessor>();
		}

		/// <summary>
		/// Tries to get a template data accessor for the specified variable.
		/// </summary>
		/// <param name="variable">The variable name to get.</param>
		/// <returns>A template data accessor if the variable exists; otherwise, null.</returns>
		public TemplateDataAccessor? TryGetVariable(string variable)
		{
			if (string.IsNullOrEmpty(variable))
				return null;
			if (_variables.TryGetValue(variable, out var data))
				return data;
			return null;
		}

		/// <summary>
		/// Sets a template data accessor for the specified variable.
		/// </summary>
		/// <param name="variable">The variable name to set.</param>
		/// <param name="data">The template data accessor to set. Must not be null.</param>
		/// <exception cref="ArgumentNullException"></exception>
		public void SetVariable(string variable, TemplateDataAccessor data)
		{
			if (string.IsNullOrEmpty(variable))
				throw new ArgumentNullException(nameof(variable));
			_variables[variable] = data ?? throw new ArgumentNullException(nameof(data));
		}

		/// <summary>
		/// Sets a template data accessor for the specified variable.
		/// </summary>
		/// <param name="variable">The variable name to set.</param>
		/// <param name="data">The template data accessor to set. Must not be null.</param>
		/// <exception cref="ArgumentNullException"></exception>
		public bool TryAssignVariable(string variable, TemplateDataAccessor data)
		{
			if (string.IsNullOrEmpty(variable))
				throw new ArgumentNullException(nameof(variable));

			if (_variables.ContainsKey(variable))
			{
				_variables[variable] = data ?? throw new ArgumentNullException(nameof(data));
				return true;
			}
			return false;
		}

		/// <summary>
		/// Removes a template data accessor for the specified variable.
		/// </summary>
		/// <param name="variable">The variable name to remove.</param>
		public void RemoveVariable(string variable)
		{
			if (string.IsNullOrEmpty(variable))
				throw new ArgumentNullException(nameof(variable));
			_variables.Remove(variable);
		}
	}

	/// <summary>
	/// The accessor for template context variables.
	/// </summary>
	public class TemplateContextAccessor : TemplateDataAccessor, IEnumerableTemplateDataAccessor
	{
		private readonly Stack<TemplateContextFrame> _frames;

		/// <summary>
		/// Gets the coxtext that have passed to render template.
		/// </summary>
		public TemplateDataAccessor Context { get; }

		/// <summary>
		/// Gets the metadata of the host template that created this context accessor.
		/// </summary>
		public IMetadataCollection HostTemplateMetadata { get; }

		/// <summary>
		/// Gets the functions that can be used in the template.
		/// </summary>
		public TemplateFunctionSet Functions { get; }

		/// <summary>
		/// Gets the library that can be used in the template for rendering inner templates.
		/// </summary>
		public TemplateLibrary Library { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="TemplateContextAccessor"/> class.
		/// </summary>
		/// <param name="context">The accessor for the template context.</param>
		/// <param name="hostTemplateMetadata">The metadata of the host template that created this context accessor.</param>
		/// <param name="functions">The set of functions that can be used in the template.</param>
		/// <param name="library">The library that can be used in the template for rendering inner templates.</param>
		/// <exception cref="ArgumentNullException"></exception>
		public TemplateContextAccessor(TemplateDataAccessor context, IMetadataCollection hostTemplateMetadata,
			TemplateFunctionSet? functions = null, TemplateLibrary? library = null)
		{
			var baseFrame = new TemplateContextFrame();
			_frames = new Stack<TemplateContextFrame>();
			_frames.Push(baseFrame);

			Context = context ?? throw new ArgumentNullException(nameof(context));
			HostTemplateMetadata = hostTemplateMetadata ?? throw new ArgumentNullException(nameof(hostTemplateMetadata));
			Functions = functions ?? TemplateFunctionSet.Default;
			Library = library ?? new TemplateLibrary();
		}

		/// <summary>
		/// Pushes a new context frame onto the stack.
		/// </summary>
		public void PushFrame()
		{
			if (_frames.Count > 1000)
				throw new TemplateRuntimeException("Stack overflow. Maximum depth exceeded.");
			_frames.Push(new TemplateContextFrame());
		}

		/// <summary>
		/// Pops the top context frame from the stack.
		/// </summary>
		public void PopFrame()
		{
			if (_frames.Count == 1)
				throw new TemplateRuntimeException("Stack underflow. No frames to pop.");
			_frames.Pop();
		}

		/// <summary>
		/// Sets a value to the specified variable onto the top context frame.
		/// </summary>
		/// <param name="variable">The variable name.</param>
		/// <param name="value">The value to set. Must not be null.</param>
		public void SetVariable(string variable, TemplateDataAccessor value)
		{
			_frames.Peek().SetVariable(variable, value);
		}

		/// <summary>
		/// Assigns a value to a specified existing variable in the context stack.
		/// </summary>
		/// <param name="variable">The variable name.</param>
		/// <param name="value">The value to set. Must not be null.</param>
		public void AssignVariable(string variable, TemplateDataAccessor value)
		{
			foreach (var frame in _frames)
				if (frame.TryAssignVariable(variable, value))
					return;

			throw new TemplateRuntimeException("Variable not found: " + variable, dataAccessor: this);
		}

		public override TemplateDataAccessor Property(string key)
		{
			foreach (var frame in _frames)
			{
				var data = frame.TryGetVariable(key);
				if (data != null)
					return data;
			}

			return Context.Property(key);
		}

		public override TemplateDataAccessor Call(string methodName, TemplateDataAccessor[] arguments)
		{
			return Functions.CallFunction(methodName, this, arguments);
		}

		public override bool AsBoolean()
		{
			return true;
		}

		public override object GetValue()
		{
			return Context.GetValue();
		}

		public override string ToString(string? format = null)
		{
			return Context.ToString(format);
		}

		/// <summary>
		/// Renders a template with the specified identifier and optional new context.
		/// </summary>
		/// <remarks>
		/// Tries to find the specified template in the local library. If not found, tries to find it in the shared library.
		/// </remarks>
		/// <param name="identifier">The identifier of the template to render.</param>
		/// <param name="newContext">The new context to use for rendering the template. If null, uses the current context.</param>
		/// <returns>The rendered template as a string.</returns>
		public string RenderTemplate(string identifier, TemplateDataAccessor? newContext)
		{
			var template = Library.TryRetrieve(identifier);
			if (template == null)
				template = TemplateLibrary.Shared.TryRetrieve(identifier); // try to get from shared library
			if (template == null)
				throw new TemplateRuntimeException($"Template '{identifier}' not found.");

			if (template is not PromptTemplate && template is not PlaintextTemplate)
				throw new TemplateRuntimeException($"Template '{identifier}' is not a text template.");

			var context = newContext ?? this;
			return template.Render(context).ToString();
		}

		/// <summary>
		/// Renders a messages template with the specified identifier and optional new context.
		/// </summary>
		/// <remarks>
		/// Tries to find the specified template in the local library. If not found, tries to find it in the shared library.
		/// </remarks>
		/// <param name="identifier">The identifier of the messages template to render.</param>
		/// <param name="newContext">The new context to use for rendering the template. If null, uses the current context.</param>
		/// <returns>The rendered template as a collection of messages.</returns>
		public IEnumerable<IMessage> RenderMessagesTemplate(string identifier, TemplateDataAccessor? newContext)
		{
			var template = Library.TryRetrieve(identifier);
			if (template == null)
				template = TemplateLibrary.Shared.TryRetrieve(identifier);
			if (template == null)
				throw new TemplateRuntimeException($"Template '{identifier}' not found.");

			if (template is not MessagesTemplate messagesTemplate)
				throw new TemplateRuntimeException($"Template '{identifier}' is not a messages template.");

			var context = newContext ?? this;
			return messagesTemplate.Render(context);
		}

		public IEnumerator<TemplateDataAccessor> GetEnumerator()
		{
			if (Context is IEnumerableTemplateDataAccessor enumerableContext)
				return enumerableContext.GetEnumerator();
			
			throw new TemplateRuntimeException("Cannot enumerate over non-enumerable context.", dataAccessor: this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}