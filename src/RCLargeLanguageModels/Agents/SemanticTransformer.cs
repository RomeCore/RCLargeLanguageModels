using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Exceptions;
using RCLargeLanguageModels.Messages;

namespace RCLargeLanguageModels.Agents
{
	/// <summary>
	/// Represents a transformer for converting data using LLM.
	/// </summary>
	/// <remarks>
	/// This is abstract class that implements the <see cref="ISemanticTransformer"/> interface.
	/// </remarks>
	public abstract class SemanticTransformer : ISemanticTransformer
	{
		public abstract Task<string> TransformAsync(string input, CancellationToken cancellationToken = default);



		private class PassThroughTransformer : SemanticTransformer
		{
			public override Task<string> TransformAsync(string input, CancellationToken cancellationToken = default)
			{
				return Task.FromResult(input); // Pass through the input without any transformation.
			}
		}

		/// <summary>
		/// A pre-defined instance of <see cref="SemanticTransformer"/> that simply passes through the input without any transformation.
		/// </summary>
		public static SemanticTransformer PassThrough { get; } = new PassThroughTransformer();
	}
}