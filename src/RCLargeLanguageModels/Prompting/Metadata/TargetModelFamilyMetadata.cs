using RCLargeLanguageModels.Metadata;

namespace RCLargeLanguageModels.Prompting.Metadata
{
	/// <summary>
	/// The metadata for target model family-related information.
	/// </summary>
	public class TargetModelFamilyMetadata : IMetadata
	{
		/// <summary>
		/// Gets the name of the target model family associated with this metadata.
		/// </summary>
		public string FamilyName { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TargetModelFamilyMetadata"/> class with the specified model family name.
		/// </summary>
		/// <param name="familyName">The name of the target model family associated with this metadata.</param>
		public TargetModelFamilyMetadata(string familyName)
		{
			FamilyName = familyName;
		}
	}
}