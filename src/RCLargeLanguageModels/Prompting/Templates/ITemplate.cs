using System;
using System.Collections.Generic;
using System.Text;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Metadata;

namespace RCLargeLanguageModels.Prompting.Templates
{
	/// <summary>
	/// Interface for a template that can be used to generate prompts.
	/// </summary>
	public interface ITemplate
	{
		/// <summary>
		/// Gets the metadata associated with this template.
		/// </summary>
		public IMetadataCollection Metadata { get; }

		/// <summary>
		/// Renders the template with the given context.
		/// </summary>
		/// <param name="context">The context to use for rendering the template. Can be null.</param>
		/// <returns>The result of rendering the template.</returns>
		object Render(object? context = null);
	}
	
	/// <summary>
	/// Interface for a template that can be used to generate prompts.
	/// </summary>
	/// <typeparam name="TResult">The type of result produced by the template.</typeparam>
	public interface ITemplate<TResult> : ITemplate
	{
		/// <inheritdoc cref="ITemplate.Render(object?)"/>
		new TResult Render(object? context = null);
	}

	/// <summary>
	/// Represents a prompt template that produces a string result.
	/// </summary>
	public interface IPromptTemplate : ITemplate<string>
	{
	}

	/// <summary>
	/// Represents a messages template that produces a collection of messages.
	/// </summary>
	public interface IMessagesTemplate : ITemplate<IEnumerable<IMessage>>
	{
	}
}