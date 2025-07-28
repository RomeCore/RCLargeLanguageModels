using Newtonsoft.Json.Linq;

namespace RCLargeLanguageModels.Completions
{
	/// <summary>
	/// The properties used to configure LLM response generation in chat and general completions.
	/// </summary>
	public interface ICompletionProperties
	{
		/// <summary>
		/// The temperature of the model in range of 0 to 1. Increasing the temperature will make the model answer more creatively.
		/// </summary>
		float? Temperature { get; set; }

		/// <summary>
		/// The top-p (nucleus sampling) parameter in range of 0 to 1. A higher value will lead to more diverse text.
		/// </summary>
		float? TopP { get; set; }

		/// <summary>
		/// The stop sequences collection that will stop the output.
		/// </summary>
		StopSequenceCollection Stop { get; set; }

		/// <summary>
		/// The maximum number of tokens to generate.
		/// </summary>
		int? MaxTokens { get; set; }
	}

	/// <summary>
	/// The general implementation of properties used to configure LLM response generation in chat and general completions.
	/// </summary>
	public class CompletionProperties : NotifyPropertyChanged, ICompletionProperties
	{
		private float? _temperature;
		/// <inheritdoc/>
		public float? Temperature { get => _temperature; set => SetAndRaise(ref _temperature, value); }

		private float? _topP;
		/// <inheritdoc/>
		public float? TopP { get => _topP; set => SetAndRaise(ref _topP, value); }

		private StopSequenceCollection _stopSequences;
		/// <inheritdoc/>
		public StopSequenceCollection Stop { get => _stopSequences; set => SetAndRaise(ref _stopSequences, value); }

		private int? _maxTokens;
		/// <inheritdoc/>
		public int? MaxTokens { get => _maxTokens; set => SetAndRaise(ref _maxTokens, value); }
	}
}