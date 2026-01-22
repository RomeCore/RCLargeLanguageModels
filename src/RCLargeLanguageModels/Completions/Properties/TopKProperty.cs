namespace RCLargeLanguageModels.Completions.Properties
{
	/// <summary>
	/// Limits sampling to the K most probable tokens.
	/// Lower values make output more deterministic.
	/// </summary>
	public sealed class TopKProperty : IntCompletionProperty
	{
		public override string Name => "top_k";
		public override int MinValue => 0;
		public override int MaxValue => int.MaxValue;

		public TopKProperty(int value) : base(value) { }
	}

}