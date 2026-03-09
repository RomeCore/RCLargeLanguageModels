using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Agents
{
	/// <summary>
	/// Represents a transformer for converting textual data.
	/// </summary>
	public interface ISemanticTransformer
	{
		/// <summary>
		/// Transforms the given input data asynchronously.
		/// </summary>
		/// <param name="input">The input data to transform.</param>
		/// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
		/// <returns>The transformed data as a string.</returns>
		Task<string> TransformAsync(string input, CancellationToken cancellationToken = default);
	}
}