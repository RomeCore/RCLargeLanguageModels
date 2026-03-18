namespace RCLargeLanguageModels.Messages.Attachments
{
	/// <summary>
	/// Represents an attachment that have an URI source.
	/// </summary>
	public interface IUriAttachment : IAttachment
	{
		/// <summary>
		/// Gets the URI of the attachment.
		/// </summary>
		string Uri { get; }
	}
}