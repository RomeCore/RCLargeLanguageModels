namespace RCLargeLanguageModels.Completions.Properties
{
	/// <summary>
	/// Number of previous tokens considered to prevent repetition.
	/// 0 disables repetition penalty, -1 uses full context window.
	/// </summary>
	public sealed class RepeatLastNProperty : IntCompletionProperty
	{
		public override string Name => "repeat_last_n";
		public override int MinValue => -1;
		public override int MaxValue => int.MaxValue;

		public RepeatLastNProperty(int value) : base(value) { }
	}

}