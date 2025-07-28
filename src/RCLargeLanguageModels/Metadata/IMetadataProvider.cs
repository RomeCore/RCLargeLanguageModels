using System;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Metadata
{
	/// <summary>
	/// Represents an interface for objects that have metadata.
	/// </summary>
	public interface IMetadataProvider
	{
		/// <summary>
		/// Gets the metadata collection associated with this object.
		/// </summary>
		public IMetadataCollection Metadata { get; }
	}

	/// <summary>
	/// Contains extension methods for <see cref="IMetadataProvider"/>.
	/// </summary>
	public static partial class MetadataExtensions
	{
		/// <summary>
		/// Gets the first metadata item of specified type, or null if not found.
		/// </summary>
		/// <typeparam name="T">The specific metadata type to retrieve.</typeparam>
		/// <param name="provider">The metadata provider.</param>
		/// <returns>The metadata instance or null.</returns>
		public static T? GetMetadata<T>(this IMetadataProvider provider)
			where T : IMetadata
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			return provider.Metadata.TryGet<T>();
		}

		/// <summary>
		/// Gets all metadata items of specified type.
		/// </summary>
		/// <typeparam name="T">The specific metadata type to retrieve.</typeparam>
		/// <param name="provider">The metadata provider.</param>
		/// <returns>An enumerable of matching metadata items.</returns>
		public static IEnumerable<T> GetAllMetadata<T>(this IMetadataProvider provider)
			where T : IMetadata
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			return provider.Metadata.GetAll<T>();
		}

		/// <summary>
		/// Checks if the provider has any metadata of the specified type.
		/// </summary>
		/// <typeparam name="T">The metadata type to check for.</typeparam>
		/// <param name="provider">The metadata provider.</param>
		/// <returns>True if at least one metadata item exists, false otherwise.</returns>
		public static bool HasMetadata<T>(this IMetadataProvider provider)
			where T : IMetadata
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			return provider.Metadata.Has<T>();
		}

		/// <summary>
		/// Gets required metadata of specified type or throws an exception if not found.
		/// </summary>
		/// <typeparam name="T">The specific metadata type to retrieve.</typeparam>
		/// <param name="provider">The metadata provider.</param>
		/// <param name="exceptionMessage">The exception message to use if metadata is not found.</param>
		/// <returns>The metadata instance.</returns>
		/// <exception cref="InvalidOperationException">Thrown when metadata is not found.</exception>
		public static T RequireMetadata<T>(this IMetadataProvider provider, string exceptionMessage)
			where T : IMetadata
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			return provider.Metadata.Require<T>(exceptionMessage);
		}

		/// <summary>
		/// Checks if the first metadata item of specified type matches the predicate.
		/// </summary>
		/// <typeparam name="T">The specific metadata type to check.</typeparam>
		/// <param name="provider">The metadata provider.</param>
		/// <param name="predicate">The predicate to test against the metadata.</param>
		/// <param name="fallback">The value to return if metadata is not found or predicate fails.</param>
		/// <returns>Result of predicate evaluation or fallback value.</returns>
		public static bool CheckMetadata<T>(
			this IMetadataProvider provider,
			Func<T, bool> predicate,
			bool fallback = false)
			where T : IMetadata
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			return provider.Metadata.Check(predicate, fallback);
		}

		/// <summary>
		/// Gets metadata value or a default value if not found.
		/// </summary>
		/// <typeparam name="T">The specific metadata type to retrieve.</typeparam>
		/// <param name="provider">The metadata provider.</param>
		/// <param name="defaultValue">The default value to return if metadata is not found.</param>
		/// <returns>The metadata instance or default value.</returns>
		public static T GetMetadataOrDefault<T>(
			this IMetadataProvider provider,
			T defaultValue)
			where T : IMetadata
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			return provider.Metadata.TryGet<T>() ?? defaultValue;
		}

		/// <summary>
		/// Gets metadata value or a default value if not found.
		/// </summary>
		/// <typeparam name="T">The specific metadata type to retrieve.</typeparam>
		/// <param name="provider">The metadata provider.</param>
		/// <param name="defaultValueFactory">Factory for default value if metadata is not found.</param>
		/// <returns>The metadata instance or default value.</returns>
		public static T GetMetadataOrDefault<T>(
			this IMetadataProvider provider,
			Func<T> defaultValueFactory)
			where T : IMetadata
		{
			if (provider == null)
				throw new ArgumentNullException(nameof(provider));
			if (defaultValueFactory == null)
				throw new ArgumentNullException(nameof(defaultValueFactory));

			return provider.Metadata.TryGet<T>() ?? defaultValueFactory();
		}
	}
}