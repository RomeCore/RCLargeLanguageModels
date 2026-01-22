namespace RCLargeLanguageModels.Completions.Properties
{
	/// <summary>
	/// Seed for the random number generator.
	/// Using the same seed with the same prompt produces identical output.
	/// </summary>
	public sealed class SeedProperty : IntCompletionProperty
	{
		public override string Name => "seed";
		public override int MinValue => 0;
		public override int MaxValue => int.MaxValue;

		public SeedProperty(int value) : base(value) { }
	}

}