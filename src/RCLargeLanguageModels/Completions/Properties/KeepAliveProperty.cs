using System;

namespace RCLargeLanguageModels.Completions.Properties
{
	/// <summary>
	/// Controls how long the model stays loaded in memory.
	/// </summary>
	public sealed class KeepAliveProperty : CompletionProperty<TimeSpan>
	{
		public override string Name => "keep_alive";

		public KeepAliveProperty(TimeSpan value) : base(value)
		{
		}
	}
}