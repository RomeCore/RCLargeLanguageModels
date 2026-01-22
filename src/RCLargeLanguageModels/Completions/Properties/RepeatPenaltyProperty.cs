namespace RCLargeLanguageModels.Completions.Properties
{
	/// <summary>
	/// Penalizes repeated tokens during generation.
	/// Values greater than 1.0 increase the penalty.
	/// </summary>
	public sealed class RepeatPenaltyProperty : FloatCompletionProperty
	{
		public override string Name => "repeat_penalty";
		public override float MinValue => 0.0f;
		public override float MaxValue => 2.0f;

		public RepeatPenaltyProperty(float value) : base(value) { }
	}

}