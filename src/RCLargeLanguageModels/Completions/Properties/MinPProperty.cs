namespace RCLargeLanguageModels.Completions.Properties
{
	/// <summary>
	/// Filters out tokens whose probability is below
	/// a fraction of the most likely token's probability.
	/// Alternative to Top-P sampling.
	/// </summary>
	public sealed class MinPProperty : FloatCompletionProperty
	{
		public override string Name => "min_p";
		public override float MinValue => 0.0f;
		public override float MaxValue => 1.0f;

		public MinPProperty(float value) : base(value) { }
	}

}