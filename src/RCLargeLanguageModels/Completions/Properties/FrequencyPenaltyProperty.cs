namespace RCLargeLanguageModels.Completions.Properties
{
	/// <summary>
	/// Penalizes tokens proportionally to their frequency
	/// in the generated text so far.
	/// </summary>
	public sealed class FrequencyPenaltyProperty : FloatCompletionProperty
	{
		public override string Name => "frequency_penalty";
		public override float MinValue => -2.0f;
		public override float MaxValue => 2.0f;

		public FrequencyPenaltyProperty(float value) : base(value) { }
	}

}