namespace RCLargeLanguageModels.Completions.Properties
{
	/// <summary>
	/// Enables internal reasoning or thinking mode if supported by the model.
	/// </summary>
	public sealed class ThinkProperty : CompletionProperty<bool>
	{
		public override string Name => "think";

		public ThinkProperty(bool value) : base(value)
		{
		}
	}
}