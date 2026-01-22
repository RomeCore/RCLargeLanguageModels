namespace RCLargeLanguageModels.Completions.Properties
{
	/// <summary>
	/// Controls randomness of the generated text.
	/// Higher values make output more creative and diverse,
	/// lower values make it more deterministic.
	/// </summary>
	public sealed class TemperatureProperty : FloatCompletionProperty
	{
		public override string Name => "temperature";
		public override float MinValue => 0.0f;
		public override float MaxValue => 1.0f;

		public TemperatureProperty(float value) : base(value) { }
	}

}