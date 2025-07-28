using System;

namespace RCLargeLanguageModels.Metadata
{
	/// <summary>
	/// Represents an interface for metadata collection with specified base metadata type.
	/// </summary>
	public interface IMetadataCollection : ITypeDictionary<IMetadata>
	{
		/// <summary>
		/// Checks that the first element of <typeparamref name="T"/> is follows the given predicate.
		/// </summary>
		/// <typeparam name="T">The type of value to check.</typeparam>
		/// <param name="predicate">The predicate. Any exceptions inside it will be swallowed.</param>
		/// <param name="fallback">
		/// The fallback value to return if there is no one element for given type
		/// or when errors be catched while processing <paramref name="predicate"/>.
		/// </param>
		/// <returns>Result of <paramref name="predicate"/> or <paramref name="fallback"/> value.</returns>
		public bool Check<T>(Func<T, bool> predicate, bool fallback = false)
			where T : IMetadata;
	}
}