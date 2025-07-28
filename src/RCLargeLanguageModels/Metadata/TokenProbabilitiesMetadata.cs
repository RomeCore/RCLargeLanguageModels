using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Metadata
{
	/// <summary>
	/// Represents probability information for a single generated token, including both the chosen token
	/// and its alternative possibilities at this position in the completion.
	/// </summary>
	/// <remarks>
	/// This metadata provides detailed information about the model's confidence in generated tokens,
	/// including both logarithmic and linear probability scales. The top alternative tokens are provided
	/// when available to help analyze the model's decision process.
	/// </remarks>
	public interface ITokenProbabilitiesMetadata : IPartialCompletionMetadata, ITokenProbability
	{
		/// <summary>
		/// Gets the list of top alternative tokens and their probabilities at this position.
		/// </summary>
		/// <value>
		/// A read-only list of <see cref="ITokenProbability"/> instances representing the most probable
		/// alternative tokens at this position, ordered from highest to lowest probability.
		/// </value>
		IReadOnlyList<ITokenProbability> TopProbabilities { get; }

		/// <summary>
		/// Gets the character offset where this token begins in the completion text.
		/// </summary>
		/// <value>
		/// The zero-based index indicating where this token starts in the complete generated text.
		/// </value>
		int TextOffset { get; }
	}

	/// <summary>
	/// Represents the probability information for a single token in a language model's output.
	/// </summary>
	/// <remarks>
	/// Provides both logarithmic and linear probability measures for consistent token generation analysis.
	/// Logarithmic probabilities are typically used for mathematical operations, while linear probabilities
	/// are more intuitive for human interpretation.
	/// </remarks>
	public interface ITokenProbability
	{
		/// <summary>
		/// Gets the text content of the token.
		/// </summary>
		/// <value>
		/// The string representation of the token as it appears in the generated text.
		/// </value>
		string Token { get; }

		/// <summary>
		/// Gets the natural logarithm of the token's probability.
		/// </summary>
		/// <value>
		/// A negative number ranging from 0 (certain) to negative infinity (impossible).
		/// For example, a log probability of -0.5 corresponds to a linear probability of about 0.6065.
		/// </value>
		double LogProbability { get; }

		/// <summary>
		/// Gets the linear probability of the token.
		/// </summary>
		/// <value>
		/// A value between 0 (impossible) and 1 (certain), representing the model's confidence
		/// that this was the correct token to generate at this position.
		/// </value>
		double Probability { get; }
	}

	/// <summary>
	/// Standard implementation of <see cref="ITokenProbabilitiesMetadata"/> containing probability
	/// information for a generated token and its alternatives.
	/// </summary>
	public class TokenProbabilitiesMetadata : ITokenProbabilitiesMetadata
	{
		/// <inheritdoc/>
		public string Token { get; }

		/// <inheritdoc/>
		public double LogProbability { get; }

		/// <inheritdoc/>
		public double Probability { get; }

		/// <inheritdoc/>
		public IReadOnlyList<ITokenProbability> TopProbabilities { get; }

		/// <inheritdoc/>
		public int TextOffset { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TokenProbabilitiesMetadata"/> class.
		/// </summary>
		/// <param name="token">The generated token text.</param>
		/// <param name="logProbability">The log probability of the token.</param>
		/// <param name="topProbabilities">
		/// Optional list of top alternative tokens and their probabilities.
		/// </param>
		/// <param name="textOffset">
		/// The character offset where this token begins in the completion.
		/// </param>
		public TokenProbabilitiesMetadata(
			string token,
			double logProbability,
			IReadOnlyList<ITokenProbability>? topProbabilities,
			int textOffset = -1)
		{
			if (logProbability > 0)
				throw new ArgumentOutOfRangeException(nameof(logProbability));

			Token = token;
			LogProbability = logProbability;
			Probability = Math.Exp(logProbability);
			TopProbabilities = topProbabilities ?? Array.Empty<ITokenProbability>();
			TextOffset = textOffset;
		}
	}

	/// <summary>
	/// Standard implementation of <see cref="ITokenProbability"/> representing a single token's
	/// probability information.
	/// </summary>
	public class TokenProbability : ITokenProbability
	{
		/// <inheritdoc/>
		public string Token { get; }

		/// <inheritdoc/>
		public double LogProbability { get; }

		/// <inheritdoc/>
		public double Probability { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TokenProbability"/> class.
		/// </summary>
		/// <param name="token">The token text.</param>
		/// <param name="logProbability">The log probability of the token.</param>
		public TokenProbability(string token, double logProbability)
		{
			if (logProbability > 0)
				throw new ArgumentOutOfRangeException(nameof(logProbability));

			Token = token;
			LogProbability = logProbability;
			Probability = Math.Exp(logProbability);
		}
	}
}