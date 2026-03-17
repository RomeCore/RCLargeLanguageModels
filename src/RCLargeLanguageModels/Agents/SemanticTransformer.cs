using System;
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
	/// This is abstract class that implements the <see cref="ISemanticTransformer{TIn, TOut}"/> interface.
	/// </remarks>
	public abstract class SemanticTransformer<TIn, TOut> : ISemanticTransformer<TIn, TOut>
	{
		public abstract Task<TOut> TransformAsync(TIn input, CancellationToken cancellationToken = default);



		private class PassThroughTransformer : SemanticTransformer<TIn, TOut>
		{
			public override Task<TOut> TransformAsync(TIn input, CancellationToken cancellationToken = default)
			{
				// Pass through the input without any transformation.
				if (input == null)
					return Task.FromResult<TOut>(default);
				if (input is TOut result)
					return Task.FromResult(result);
				throw new InvalidCastException($"Cannot cast object of type {input?.GetType()} to {typeof(TOut)} in pass-through transformer.");
			}
		}

		/// <summary>
		/// A pre-defined instance of <see cref="ISemanticTransformer{TIn, TOut}"/> that simply passes through the input without any transformation.
		/// </summary>
		public static SemanticTransformer<TIn, TOut> PassThrough { get; } = new PassThroughTransformer();
	
		
	}
}