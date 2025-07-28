using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace RCLargeLanguageModels
{
	public partial class LLMClient
	{
		/// <summary>
		/// Lists the available models.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>An array of <see cref="LLModelDescriptor"/> objects that represents available models.</returns>
		protected abstract Task<LLModelDescriptor[]> ListModelsOverrideAsync(
			CancellationToken cancellationToken);

		/// <summary>
		/// Lists the available models via descriptors, asynchronously.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>An array of <see cref="LLModelDescriptor"/> objects that represents available models.</returns>
		public async Task<LLModelDescriptor[]> ListModelDescriptorsAsync(CancellationToken cancellationToken = default)
		{
			return await ListModelsOverrideAsync(cancellationToken);
		}

		/// <summary>
		/// Lists the available models asynchronously.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>An array of <see cref="LLModel"/> objects that represents available models.</returns>
		public async Task<LLModel[]> ListModelsAsync(CancellationToken cancellationToken = default)
		{
			return (await ListModelsOverrideAsync(cancellationToken))
				.Select(d => new LLModel(d))
				.ToArray();
		}

		/// <summary>
		/// Lists the available models via descriptors using the specified capability filter, asynchronously.
		/// </summary>
		/// <param name="filter">The model capability filter.</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>An array of <see cref="LLModelDescriptor"/> objects that represents available models.</returns>
		public async Task<LLModelDescriptor[]> ListModelDescriptorsAsync(LLMCapabilities filter, CancellationToken cancellationToken = default)
		{
			return (await ListModelsOverrideAsync(cancellationToken))
				.Where(m => (m.Capabilities & filter) > 0)
				.ToArray();
		}

		/// <summary>
		/// Lists the available models using the specified capability filter, asynchronously.
		/// </summary>
		/// <param name="filter">The model capability filter.</param>
		/// <param name="cancellationToken">The cancellation token used to cancel the operation.</param>
		/// <returns>An array of <see cref="LLModel"/> objects that represents available models.</returns>
		public async Task<LLModel[]> ListModelsAsync(LLMCapabilities filter, CancellationToken cancellationToken = default)
		{
			return (await ListModelsOverrideAsync(cancellationToken))
				.Where(m => (m.Capabilities & filter) > 0)
				.Select(d => new LLModel(d))
				.ToArray();
		}
	}
}