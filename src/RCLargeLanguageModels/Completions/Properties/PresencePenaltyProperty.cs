namespace RCLargeLanguageModels.Completions.Properties
{
	/// <summary>
	/// Penalizes tokens based on whether they already appeared
	/// in the generated text.
	/// </summary>
	public sealed class PresencePenaltyProperty : FloatCompletionProperty
	{
		public override string Name => "presence_penalty";
		public override float MinValue => -2.0f;
		public override float MaxValue => 2.0f;

		public PresencePenaltyProperty(float value) : base(value) { }
	}

}