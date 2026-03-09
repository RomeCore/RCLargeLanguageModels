namespace RCLargeLanguageModels.Completions.Properties
{
	/// <summary>
	/// Specifies sequences at which text generation will stop.
	/// </summary>
	public sealed class StopSequencesProperty : CompletionProperty<StopSequenceCollection>
	{
		public override string Name => "stop";

		public StopSequencesProperty(StopSequenceCollection value) : base(value)
		{
		}
	}
}