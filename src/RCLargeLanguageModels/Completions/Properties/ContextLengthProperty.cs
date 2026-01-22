namespace RCLargeLanguageModels.Completions.Properties
{
	/// <summary>
	/// Size of the context window used for token generation.
	/// Defines how many tokens the model can attend to.
	/// </summary>
	public sealed class ContextLengthProperty : IntCompletionProperty
	{
		public override string Name => "num_ctx";
		public override int MinValue => 1;
		public override int MaxValue => int.MaxValue;

		public ContextLengthProperty(int value) : base(value) { }
	}

}