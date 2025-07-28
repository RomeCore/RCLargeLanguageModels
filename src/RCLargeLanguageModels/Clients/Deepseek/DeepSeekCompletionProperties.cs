using System;
using System.Collections.Generic;
using RCLargeLanguageModels.Completions;

namespace RCLargeLanguageModels.Clients.Deepseek
{
	/// <summary>
	/// The completion properties to use with DeepSeek client.
	/// </summary>
	public class DeepSeekCompletionProperties : NotifyPropertyChanged, ICompletionProperties
	{
		private float? _temperature;
		private float? _topP;
		private int? _maxTokens;
		private StopSequenceCollection _stopSequences;
		private float? _frequencyPenalty;
		private float? _presencePenalty;

		/// <summary>
		/// The temperature of the model in the range of 0 to 2. Increasing the temperature will make the model answer more creatively. (Default: 1.0)
		/// </summary>
		public float? Temperature
		{
			get => _temperature;
			set => SetAndRaise(ref _temperature, value);
		}
		float? ICompletionProperties.Temperature
		{
			get => Temperature == null ? null : Temperature / 2;
			set => Temperature = value == null ? null : value * 2;
		}

		/// <summary>
		/// The top-p (nucleus sampling) parameter. A higher value (e.g., 0.95) will lead to more diverse text. (Default: 1.0)
		/// </summary>
		public float? TopP
		{
			get => _topP;
			set => SetAndRaise(ref _topP, value);
		}

		/// <summary>
		/// The maximum number of tokens to generate. (Default: 4096, Max: 8192)
		/// </summary>
		public int? MaxTokens
		{
			get => _maxTokens;
			set => SetAndRaise(ref _maxTokens, value);
		}

		/// <summary>
		/// Up to 16 sequences where the API will stop generating further tokens.
		/// </summary>
		public StopSequenceCollection Stop
		{
			get => _stopSequences;
			set => SetAndRaise(ref _stopSequences, value);
		}

		/// <summary>
		/// Positive values penalize new tokens based on their existing frequency in the text so far. (Default: 0.0)
		/// </summary>
		public float? FrequencyPenalty
		{
			get => _frequencyPenalty;
			set => SetAndRaise(ref _frequencyPenalty, value);
		}

		/// <summary>
		/// Positive values penalize new tokens based on whether they appear in the text so far. (Default: 0.0)
		/// </summary>
		public float? PresencePenalty
		{
			get => _presencePenalty;
			set => SetAndRaise(ref _presencePenalty, value);
		}
	}
}