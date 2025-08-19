using RCLargeLanguageModels.Metadata;

namespace RCLargeLanguageModels.Prompting.Metadata
{
	/// <summary>
	/// The metadata for target model-related information.
	/// </summary>
	public class TargetModelMetadata : IMetadata
	{
		/// <summary>
		/// Gets the name of the target model associated with this metadata.
		/// </summary>
		public string ModelName { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TargetModelMetadata"/> class with the specified model name.
		/// </summary>
		/// <param name="modelName">The name of the target model associated with this metadata.</param>
		public TargetModelMetadata(string modelName)
		{
			ModelName = modelName;
		}

		public override bool Equals(object? obj)
		{
			return obj is TargetModelMetadata other && ModelName == other.ModelName;
		}

		public override int GetHashCode()
		{
			int hash = 17;
			hash *= 397 + ModelName.GetHashCode();
			return hash;
		}
	}
}