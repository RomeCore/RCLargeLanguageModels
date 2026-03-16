using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tools;

namespace RCLargeLanguageModels.Statistics
{
	/// <summary>
	/// The class that conteins extenson methods for <see cref="ITokenCounter"/>.
	/// </summary>
	public static class TokenCounterExtensions
	{
		/// <summary>
		/// Counts tokens in message.
		/// </summary>
		/// <param name="counter"></param>
		/// <param name="message"></param>
		/// <param name="availableTools"></param>
		/// <param name="serializationSchema"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public static Task<int> CountAsync(this ITokenCounter counter, IMessage message,
			IEnumerable<ITool>? availableTools = null,
			IMessageTokenSerializationSchema? serializationSchema = null,
			CancellationToken cancellationToken = default)
		{
			availableTools ??= Enumerable.Empty<ITool>();
			serializationSchema ??= MessageTokenSerializationSchema.Default;
			var text = serializationSchema.SerializeMessage(message, availableTools);
			return counter.CountAsync(text, cancellationToken);
		}
	}
}