using System;
using System.Collections.Generic;
using System.Text;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Metadata;

namespace RCLargeLanguageModels.Prompting.Templates
{
	/// <summary>
	/// Represents a prompt template that produces a collection of messages.
	/// </summary>
	public class MessagesTemplate : ITemplate
	{
		private readonly MessagesTemplateNode _node;

		public IMetadataCollection Metadata { get; }

		/// <summary>
		/// Gets the local library associated with this prompt template.
		/// </summary>
		public TemplateLibrary LocalLibrary { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MessagesTemplate"/> class.
		/// </summary>
		/// <param name="mainNode">The main node of the template.</param>
		/// <param name="metadata">The metadata associated with this template.</param>
		/// <param name="localLibrary">The local library associated with this prompt template.</param>
		/// <exception cref="ArgumentNullException"></exception>
		public MessagesTemplate(MessagesTemplateNode mainNode, IMetadataCollection metadata, TemplateLibrary localLibrary)
		{
			_node = mainNode ?? throw new ArgumentNullException(nameof(mainNode));
			Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
			LocalLibrary = localLibrary ?? throw new ArgumentNullException(nameof(localLibrary));
		}

		/// <summary>
		/// Renders the messages template using the provided data accessor.
		/// </summary>
		/// <param name="context">The context accessor to use for rendering.</param>
		/// <returns>The rendered prompt as a collection of messages.</returns>
		public IEnumerable<IMessage> Render(object? context = null)
		{
			var ctx = new TemplateContextAccessor(TemplateDataAccessor.Create(context), Metadata, library: LocalLibrary);
			return _node.Render(ctx);
		}

		object ITemplate.Render(object? context)
		{
			var ctx = new TemplateContextAccessor(TemplateDataAccessor.Create(context), Metadata, library: LocalLibrary);
			return _node.Render(ctx);
		}
	}
}
