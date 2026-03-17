using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Agents
{
	/// <summary>
	/// Represents a transformer for converting data via LLM.
	/// </summary>
	public interface ISemanticTransformer<TIn, TOut>
	{
		/// <summary>
		/// Transforms the given input data asynchronously.
		/// </summary>
		/// <param name="input">The input data to transform.</param>
		/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
		/// <returns>The transformed data.</returns>
		Task<TOut> TransformAsync(TIn input, CancellationToken cancellationToken = default);
	}
}