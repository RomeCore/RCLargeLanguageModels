using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace RCLargeLanguageModels.Embeddings.Database
{
	/// <summary>
	/// Represents a semantic database that stores and retrieves data by semantic similarity.
	/// </summary>
	public class SemanticDatabase
	{
		private readonly string _homePath;
		private readonly LLModel _embeddingModel;

		/// <summary>
		/// Gets the embedding model associated with this database.
		/// </summary>
		public LLModel EmbeddingModel => _embeddingModel;

		/// <summary>
		/// Initializes a new instance of the <see cref="SemanticDatabase"/> class.
		/// </summary>
		/// <param name="homePath">The path to the database's home directory. This should be a writable location.</param>
		/// <param name="embeddingModel">The model used for generating embeddings.</param>
		public SemanticDatabase(string homePath, LLModel embeddingModel)
		{
			_homePath = homePath ?? throw new ArgumentNullException(nameof(homePath));
			_embeddingModel = embeddingModel ?? throw new ArgumentNullException(nameof(embeddingModel));
		}

		/// <summary>
		/// Creates a semantic sector with the specified name and properties.
		/// </summary>
		/// <typeparam name="T">The type of data stored in the sector.</typeparam>
		/// <param name="name">The name of the sector. This must be unique within the database.</param>
		/// <param name="properties">The properties of the sector. If not provided, default properties will be used.</param>
		/// <returns>The created sector.</returns>
		public SemanticSector<T> CreateSector<T>(string name, SemanticSectorProperties<T>? properties = null)
		{
			var path = Path.Combine(_homePath, name);
			properties ??= new SemanticSectorProperties<T>();
			return new SemanticSector<T>(this, properties, path);
		}
	}
}