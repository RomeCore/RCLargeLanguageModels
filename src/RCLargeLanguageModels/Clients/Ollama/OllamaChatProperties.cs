using System;
using RCLargeLanguageModels.Completions;

namespace RCLargeLanguageModels.Clients.Ollama
{
	public class OllamaChatProperties : NotifyPropertyChanged, ICompletionProperties
	{
		private TimeSpan? _keepAlive;
		private bool? _think;
		private int? _numContext;
		private int? _repeatLastN;
		private float? _repeatPenalty;
		private float? _temperature;
		private int? _seed;
		private StopSequenceCollection _stopSequences;
		private int? _numPredict;
		private int? _topK;
		private float? _topP;
		private float? _minP;

		/// <summary>
		/// The time model is kept loaded. (default: 5 mins)
		/// </summary>
		public TimeSpan? KeepAlive
		{
			get => _keepAlive;
			set => SetAndRaise(ref _keepAlive, value);
		}

		/// <summary>
		/// Enable thinking mode. Introduced in Ollama 0.9.0 (Default: true)
		/// </summary>
		public bool? Think
		{
			get => _think;
			set => SetAndRaise(ref _think, value);
		}

		/// <summary>
		/// Sets the size of the context window used to generate the next token. (Default: 2048)
		/// </summary>
		public int? NumContext
		{
			get => _numContext;
			set => SetAndRaise(ref _numContext, value);
		}

		/// <summary>
		/// Sets how far back for the model to look back to prevent repetition.
		/// (Default: 64, 0 = disabled, -1 = <see cref="NumContext"/>)
		/// </summary>
		public int? RepeatLastN
		{
			get => _repeatLastN;
			set => SetAndRaise(ref _repeatLastN, value);
		}

		/// <summary>
		/// Sets how strongly to penalize repetitions.
		/// A higher value (e.g., 1.5) will penalize repetitions more strongly,
		/// while a lower value (e.g., 0.9) will be more lenient. (Default: 1.1)
		/// </summary>
		public float? RepeatPenalty
		{
			get => _repeatPenalty;
			set => SetAndRaise(ref _repeatPenalty, value);
		}

		/// <summary>
		/// The temperature of the model.
		/// Increasing the temperature will make the model answer more creatively. (Default: 0.8)
		/// </summary>
		public float? Temperature
		{
			get => _temperature;
			set => SetAndRaise(ref _temperature, value);
		}

		/// <summary>
		/// Sets the random number seed to use for generation.
		/// Setting this to a specific number will make the model
		/// generate the same text for the same prompt. (Default: 0)
		/// </summary>
		public int? Seed
		{
			get => _seed;
			set => SetAndRaise(ref _seed, value);
		}

		/// <summary>
		/// Sets the stop sequences to use.
		/// When this pattern is encountered the LLM will stop generating text and return.
		/// </summary>
		public StopSequenceCollection Stop
		{
			get => _stopSequences;
			set => SetAndRaise(ref _stopSequences, value);
		}

		/// <summary>
		/// Maximum number of tokens to predict when generating text. (Default: -1, infinite generation)
		/// </summary>
		public int? NumPredict
		{
			get => _numPredict;
			set => SetAndRaise(ref _numPredict, value);
		}

		int? ICompletionProperties.MaxTokens
		{
			get => NumPredict;
			set => NumPredict = value;
		}

		/// <summary>
		/// Reduces the probability of generating nonsense.
		/// A higher value (e.g. 100) will give more diverse answers,
		/// while a lower value (e.g. 10) will be more conservative. (Default: 40)
		/// </summary>
		public int? TopK
		{
			get => _topK;
			set => SetAndRaise(ref _topK, value);
		}

		/// <summary>
		/// Works together with <see cref="TopK"/>.
		/// A higher value (e.g., 0.95) will lead to more diverse text,
		/// while a lower value (e.g., 0.5) will generate more focused and conservative text. (Default: 0.9)
		/// </summary>
		public float? TopP
		{
			get => _topP;
			set => SetAndRaise(ref _topP, value);
		}

		/// <summary>
		/// Alternative to the <see cref="TopP"/>, and aims to ensure a balance of quality and variety.
		/// The parameter p represents the minimum probability for a token to be considered,
		/// relative to the probability of the most likely token. For example, with p=0.05
		/// and the most likely token having a probability of 0.9, logits with a value
		/// less than 0.045 are filtered out. (Default: 0.0)
		/// </summary>
		public float? MinP
		{
			get => _minP;
			set => SetAndRaise(ref _minP, value);
		}

		/// <summary>
		/// Creates an instance of <see cref="OllamaChatProperties"/> from an existing <see cref="ICompletionProperties"/> object.
		/// </summary>
		/// <param name="properties">The chat properties to convert from.</param>
		/// <returns>A new or existing <see cref="OllamaChatProperties"/> instance.</returns>
		public static OllamaChatProperties From(ICompletionProperties properties)
		{
			if (properties is OllamaChatProperties result)
				return result;

			return new OllamaChatProperties
			{
				Temperature = properties?.Temperature,
				TopP = properties?.TopP,
				NumPredict = properties?.MaxTokens,
				Stop = properties?.Stop
			};
		}
	}
}