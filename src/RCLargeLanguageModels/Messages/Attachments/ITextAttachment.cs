namespace RCLargeLanguageModels.Messages.Attachments
{
	/// <summary>
	/// Represents an attachment that contains text.
	/// </summary>
	public interface ITextAttachment : IAttachment
	{
		/// <summary>
		/// Gets the text content of the attachment.
		/// </summary>
		string Content { get; }
	}
}