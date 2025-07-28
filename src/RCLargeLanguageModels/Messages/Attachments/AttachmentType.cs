using System;
using RCLargeLanguageModels.Formats;

namespace RCLargeLanguageModels.Messages.Attachments
{
	/// <summary>
	/// Represents an attachment type.
	/// </summary>
	public readonly struct AttachmentType
	{
		/// <summary>
		/// Gets the ID of the attachment type.
		/// </summary>
		public string Id { get; }

		/// <summary>
		/// Creates a new instance of <see cref="AttachmentType"/> structure using type id.
		/// </summary>
		/// <param name="id">The ID of the attachment type.</param>
		public AttachmentType(string id)
		{
			Id = id;
		}

		public override bool Equals(object obj)
		{
			if (obj is OutputFormatType other)
				return StringComparer.OrdinalIgnoreCase.Equals(Id, other.Id);
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return StringComparer.OrdinalIgnoreCase.GetHashCode(Id);
		}

		public static bool operator ==(AttachmentType left, AttachmentType right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(AttachmentType left, AttachmentType right)
		{
			return !Equals(left, right);
		}

		public override string ToString()
		{
			return Id;
		}

		/// <summary>
		/// The text attachment type.
		/// </summary>
		public static AttachmentType Text { get; } = new AttachmentType("text");

		/// <summary>
		/// The image attachment type.
		/// </summary>
		public static AttachmentType Image { get; } = new AttachmentType("image");

		/// <summary>
		/// The audio attachment type.
		/// </summary>
		public static AttachmentType Audio { get; } = new AttachmentType("audio");

		/// <summary>
		/// The video attachment type.
		/// </summary>
		public static AttachmentType Video { get; } = new AttachmentType("video");
	}
}