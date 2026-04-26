namespace RCLargeLanguageModels.Completions.Properties
{
	/// <summary>
	/// Enables internal reasoning or thinking mode if supported by the model.
	/// </summary>
	public sealed class ReasoningProperty : CompletionProperty<bool>
	{
		public override string Name => "think";
		public ReasoningEffort Effort { get; }

		public ReasoningProperty(bool value) : base(value)
		{
			Effort = ReasoningEffort.Default;
		}

		public ReasoningProperty(ReasoningEffort effort) : base(true)
		{
			Effort = effort;
		}
	}
}