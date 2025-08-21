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

		public override string ToString()
		{
			return $"Target model family: '{FamilyName}'";
		}

		public override bool Equals(object? obj)
		{
			return obj is TargetModelFamilyMetadata other && FamilyName == other.FamilyName;
		}

		public override int GetHashCode()
		{
			int hash = 21;
			hash *= 397 + FamilyName.GetHashCode();
			return hash;
		}
	}
}