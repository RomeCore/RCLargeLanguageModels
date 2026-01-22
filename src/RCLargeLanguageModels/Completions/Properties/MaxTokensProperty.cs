namespace RCLargeLanguageModels.Completions.Properties
{
	/// <summary>
	/// Maximum number of tokens the model is allowed to generate.
	/// Also known as NumPredict in some backends.
	/// </summary>
	public sealed class MaxTokensProperty : IntCompletionProperty
	{
		public override string Name => "max_tokens";
		public override int MinValue => -1; // -1 often means "no limit"
		public override int MaxValue => int.MaxValue;

		public MaxTokensProperty(int value) : base(value) { }
	}

}