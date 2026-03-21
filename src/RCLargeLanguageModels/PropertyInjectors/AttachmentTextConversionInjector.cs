using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Formats;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Messages.Attachments;
using RCLargeLanguageModels.Tools;

namespace RCLargeLanguageModels.PropertyInjectors
{
	/// <summary>
	/// Represents a property injector that handles attachments in chat completions.
	/// </summary>
	public class AttachmentTextConversionInjector : ILLModelPropertyInjector
	{
		private readonly IAttachmentToTextConverter[] _converters;
		private readonly IAttachmentMessageFormatter _formatter;

		/// <summary>
		/// Initializes a new instance of the <see cref="AttachmentTextConversionInjector"/> class.
		/// </summary>
		/// <param name="formatter">The attachment message formatter.</param>
		public AttachmentTextConversionInjector(IAttachmentMessageFormatter? formatter = null)
		{
			_converters = Array.Empty<IAttachmentToTextConverter>();
			_formatter = formatter ?? new DefaultAttachmentMessageFormatter();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AttachmentTextConversionInjector"/> class.
		/// </summary>
		/// <param name="converters">The attachment to text converters.</param>
		/// <param name="formatter">The attachment message formatter.</param>
		public AttachmentTextConversionInjector(IEnumerable<IAttachmentToTextConverter> converters,
			IAttachmentMessageFormatter? formatter = null)
		{
			_converters = converters.ToArray();
			_formatter = formatter ?? new DefaultAttachmentMessageFormatter();
		}

		public async Task InjectChatCompletionAsync(ChatCompletionInjectionParameters parameters)
		{
			for (int i = 0; i < parameters.Messages.Count; i++)
			{
				var message = parameters.Messages[i];

				if (message is not IAttachmentsMessage attachmentsMessage)
					continue;
				if (attachmentsMessage.Attachments.Count == 0)
					continue;

				if (message is IUserMessage userMessage)
				{
					var (newContent, attachments) = await InjectAttachmentsAsync(userMessage);
					message = new UserMessage(userMessage.Sender, newContent, attachments);
				}
				else if (message is IToolMessage toolMessage)
				{
					var (newContent, attachments) = await InjectAttachmentsAsync(toolMessage);
					var newResult = new ToolResult(toolMessage.Status, newContent, attachments);
					message = new ToolMessage(newResult, toolMessage.ToolCallId, toolMessage.ToolName);
				}

				parameters.Messages[i] = message;
			}
		}

		private async Task<(string, List<IAttachment>)> InjectAttachmentsAsync(IAttachmentsMessage message)
		{
			var textAttachments = new List<ITextAttachment>();
			var otherAttachments = new List<IAttachment>();

			foreach (var attachment in message.Attachments)
			{
				if (attachment is ITextAttachment textAttachment)
					textAttachments.Add(textAttachment);

				foreach (var converter in _converters)
					if (converter.CanConvert(attachment))
						textAttachments.Add(await converter.ConvertAsync(attachment));

				otherAttachments.Add(attachment);
			}

			var content = _formatter.Format(message.Content, textAttachments);
			return (content, otherAttachments);
		}

		public Task InjectCompletionAsync(CompletionInjectionParameters parameters)
		{
			return Task.CompletedTask;
		}

		public Task InjectEmbeddingAsync(EmbeddingInjectionParameters parameters)
		{
			return Task.CompletedTask;
		}
	}

	public static class AttachmentTextConversionInjectorExtensions
	{
		/// <summary>
		/// Adds an attachment text conversion injector to the specified model.
		/// </summary>
		/// <param name="model">The model to add the injector to.</param>
		/// <param name="converters">The attachment to text converters.</param>
		/// <returns>The model with the injector added.</returns>
		public static LLModel WithTextAttachmentInjection(this LLModel model,
			params IAttachmentToTextConverter[] converters)
		{
			return model.WithInjectorAppend(new AttachmentTextConversionInjector(converters));
		}

		/// <summary>
		/// Adds an attachment text conversion injector to the specified model.
		/// </summary>
		/// <param name="model">The model to add the injector to.</param>
		/// <param name="formatter">The attachment message formatter.</param>
		/// <param name="converters">The attachment to text converters.</param>
		/// <returns>The model with the injector added.</returns>
		public static LLModel WithTextAttachmentInjection(this LLModel model,
			IAttachmentMessageFormatter? formatter, params IAttachmentToTextConverter[] converters)
		{
			return model.WithInjectorAppend(new AttachmentTextConversionInjector(converters, formatter));
		}
	}
}