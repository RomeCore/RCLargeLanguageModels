using System.IO;

namespace RCLargeLanguageModels.Messages.Attachments
{
	/// <summary>
	/// The factory class for creating attachments.
	/// </summary>
	public static class Attachment
	{
		/// <summary>
		/// Creates a new file attachment using the specified file path.
		/// </summary>
		/// <param name="filepath">The path to the file (can be absolute or relative).</param>
		/// <returns>The created attachment.</returns>
		/// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
		public static IAttachment CreateRawFile(string filepath)
		{
			var file = new FileInfo(filepath);
			if (!file.Exists)
				throw new FileNotFoundException("File not found", filepath);
			return new RawTextAttachment($"FILE {file.Name}", filepath);
		}
	}
}