using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Completions
{
	/// <summary>
	/// Represents a token and its probability information.
	/// </summary>
	public class TokenProbability
	{
		/// <summary>
		/// The token text.
		/// </summary>
		public string Token { get; }

		/// <summary>
		/// The logarithmical probability of the token.
		/// </summary>
		public double LogProb { get; }

		/// <summary>
		/// Top alternative tokens and their log probabilities.
		/// </summary>
		public ImmutableDictionary<string, double> TopAlternatives { get; }

		/// <summary>
		/// Initializes a new instance of the TokenProbability class.
		/// </summary>
		public TokenProbability(string token, double logProb, IDictionary<string, double> topAlternatives = null)
		{
			Token = token ?? throw new ArgumentNullException(nameof(token));
			LogProb = logProb;
			TopAlternatives = topAlternatives != null
				? topAlternatives.ToImmutableDictionary()
				: ImmutableDictionary.Create<string, double>();
		}

		/// <summary>
		/// Gets the actual probability (converted from log probability).
		/// </summary>
		public double Probability => Math.Exp(LogProb);

		/// <summary>
		/// Returns a string representation of the token probability.
		/// </summary>
		public override string ToString()
		{
			return $"{Token} (p={Probability:P2}, logp={LogProb:F2})";
		}
	}
}