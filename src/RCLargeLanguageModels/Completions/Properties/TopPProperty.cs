namespace RCLargeLanguageModels.Completions.Properties
{
	/// <summary>
	/// Controls nucleus sampling.
	/// Only tokens within the top cumulative probability mass are considered.
	/// </summary>
	public sealed class TopPProperty : FloatCompletionProperty
	{
		public override string Name => "top_p";
		public override float MinValue => 0.0f;
		public override float MaxValue => 1.0f;

		public TopPProperty(float value) : base(value) { }
	}
}