namespace RCLargeLanguageModels.Completions.Properties
{
	/// <summary>
	/// Enables or disables truncation of the input text if it exceeds a certain length.
	/// </summary>
	public class TruncateProperty : CompletionProperty<bool>
	{
		public override string Name => "truncate";

		public TruncateProperty(bool value) : base(value) { }
	}
}