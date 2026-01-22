namespace RCLargeLanguageModels.Completions.Properties
{
	/// <summary>
	/// Specifies sequences at which text generation will stop.
	/// </summary>
	public sealed class StopSequencesProperty : CompletionProperty<StopSequenceCollection>
	{
		public override string Name => "stop";
		public override StopSequenceCollection Value { get; }

		public StopSequencesProperty(StopSequenceCollection value)
		{
			Value = value;
		}
	}

}