// #define MEASURE_TIME

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace RCLargeLanguageModels.Embeddings.Database
{
	/// <summary>
	/// Represents a semantic sector in the database.
	/// </summary>
	/// <typeparam name="T">The type of data stored in the sector.</typeparam>
	public class SemanticSector<T>
	{
		private readonly SemanticDatabase _database;
		private readonly SemanticSectorProperties<T> _properties;
		private readonly object _lock = new();

		private readonly string _path;
		private readonly string _vectorsPath;
		private readonly string _dataPath;
		private readonly string _dataOffsetsPath;
		private readonly string _sectorMetadataPath;

		private class SectorMetadata
		{
			[JsonPropertyName("embedding_dimension")]
			public int EmbeddingDimension { get; set; }

			[JsonPropertyName("count")]
			public int Count { get; set; }

			[JsonPropertyName("model")]
			public string Model { get; set; }
		}

		private SectorMetadata _metadata;

		/// <summary>
		/// Gets the number of records stored in this sector.
		/// </summary>
		public int Count => _metadata.Count;

		internal SemanticSector(SemanticDatabase database, SemanticSectorProperties<T> properties, string path)
		{
			_database = database;
			_properties = properties;

			_path = path;
			_vectorsPath = Path.Combine(_path, "vectors.bin");
			_dataPath = Path.Combine(_path, "data.jsonl");
			_dataOffsetsPath = Path.Combine(_path, "data-offsets.bin");
			_sectorMetadataPath = Path.Combine(_path, "metadata.json");

			Directory.CreateDirectory(_path);

			if (File.Exists(_sectorMetadataPath))
			{
				var json = File.ReadAllText(_sectorMetadataPath);
				_metadata = JsonSerializer.Deserialize<SectorMetadata>(json) ?? new SectorMetadata();
			}
			else
			{
				_metadata = new SectorMetadata
				{
					EmbeddingDimension = 0, // Unknown embedding dimension initially
					Count = 0,
					Model = _database.EmbeddingModel.Descriptor.FullName
				};
				SaveMetadata();
			}
		}

		private void SaveMetadata()
		{
			var json = JsonSerializer.Serialize(_metadata, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(_sectorMetadataPath, json);
		}



		/// <summary>
		/// Records a single item in the sector.
		/// </summary>
		/// <param name="item">The item to record.</param>
		/// <param name="cancellationToken">The cancellation token to use for this operation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		public Task RecordAsync(T item, CancellationToken cancellationToken = default)
		{
			return RecordRangeAsync(new[] { item }, cancellationToken);
		}

		/// <summary>
		/// Records a collection of items into the sector.
		/// </summary>
		public async Task RecordRangeAsync(IEnumerable<T> items, CancellationToken cancellationToken = default)
		{
#if MEASURE_TIME
			var timeStart = DateTime.Now;
			TimeSpan timeTransform = TimeSpan.Zero;
			TimeSpan timeEmbeddings = TimeSpan.Zero;
#endif

			if (_metadata.Count != 0 && _database.EmbeddingModel.Descriptor.FullName != _metadata.Model)
				throw new InvalidOperationException("Embedding model has changed since the sector was created. " +
					"Use RebuildWithModelAsync() to rebuild the sector with the new model " +
					"or forcefully change the model with ForceActualizeModel() method.");

			foreach (var batch in items.EnumerateBatches(_properties.EmbeddingBatchSize))
			{
#if MEASURE_TIME
				var timePreTransform = DateTime.Now;
#endif

				var inputs = batch.Select(_properties.InputGetter).ToArray();
				var transformedTasks = inputs.Select(i => _properties.InputTransformer.TransformAsync(i, cancellationToken));
				var transformed = await Task.WhenAll(transformedTasks);

				// Get embedding from model

#if MEASURE_TIME
				var timePreEmbedding = DateTime.Now;
				timeTransform += timePreEmbedding - timePreTransform;
#endif

				EmbeddingResult embeddingResult = await _database.EmbeddingModel.EmbedAsync(transformed, cancellationToken: cancellationToken);
				var embeddings = embeddingResult.Embeddings;

#if MEASURE_TIME
				var timePostEmbedding = DateTime.Now;
				timeEmbeddings += timePostEmbedding - timePreEmbedding;
#endif

				if (_metadata.Count == 0)
					_metadata.EmbeddingDimension = embeddingResult.Embedding.Dimensions;
				else if (embeddingResult.Embedding.Dimensions != _metadata.EmbeddingDimension)
					throw new InvalidOperationException("Embedding dimension mismatch.");

				lock (_lock)
				{
					using var offsetStream = new FileStream(_dataOffsetsPath, FileMode.Append, FileAccess.Write, FileShare.None);
					using var bwOffsets = new BinaryWriter(offsetStream);

					using var dataStream = new FileStream(_dataPath, FileMode.Append, FileAccess.Write, FileShare.None);
					using var sw = new StreamWriter(dataStream);

					// Append vectors
					using (var vecStream = new FileStream(_vectorsPath, FileMode.Append, FileAccess.Write, FileShare.None))
					{
						foreach (var embedding in embeddings)
						{
							var bytes = embedding.Normalized.ToByteArray();
							vecStream.Write(bytes, 0, bytes.Length);
						}
					}

					foreach (var item in batch)
					{
						long offset = dataStream.Length;

						// Append offset
						bwOffsets.Write(offset);
						bwOffsets.Flush();

						// Append data
						string jsonLine = JsonSerializer.Serialize(item, _properties.SerializationOptions);
						sw.WriteLine(jsonLine);
						sw.Flush();

						_metadata.Count++;
					}

					SaveMetadata();
				}
			}

#if MEASURE_TIME
			var timeEnd = DateTime.Now;

			var timeTotal = timeEnd - timeStart;
			var nonEmbeddingTime = timeTotal - timeEmbeddings;

			Console.WriteLine($"Recorded {items.Count()} items in {timeTotal.TotalMilliseconds} ms. " +
				$"Transforming took {timeTransform.TotalMilliseconds} ms. " +
				$"Embedding took {timeEmbeddings.TotalMilliseconds} ms. " +
				$"Non-embedding took {nonEmbeddingTime.TotalMilliseconds} ms.");
#endif
		}

		/// <summary>
		/// Clears all records from this sector.
		/// </summary>
		public void Clear()
		{
			lock (_lock)
			{
				File.Delete(_vectorsPath);
				File.Delete(_dataPath);
				File.Delete(_dataOffsetsPath);

				_metadata.EmbeddingDimension = 0;
				_metadata.Count = 0;
				_metadata.Model = _database.EmbeddingModel.Descriptor.FullName;
				SaveMetadata();
			}
		}

		/// <summary>
		/// Gets all records from this sector.
		/// </summary>
		/// <returns>The array of records.</returns>
		public T[] GetAll()
		{
			lock (_lock)
			{
				if (!File.Exists(_dataPath))
					return Array.Empty<T>();

				var lines = File.ReadAllLines(_dataPath);
				var items = new T[lines.Length];
				for (int i = 0; i < lines.Length; i++)
				{
					items[i] = JsonSerializer.Deserialize<T>(lines[i], _properties.SerializationOptions);
				}
				return items;
			}
		}

		/// <summary>
		/// Gets all records from this sector.
		/// </summary>
		/// <returns>The enumeration of records.</returns>
		public IEnumerable<T> EnumerateAll()
		{
			lock (_lock)
			{
				if (!File.Exists(_dataPath))
					yield break;

				foreach (var line in File.ReadLines(_dataPath))
				{
					yield return JsonSerializer.Deserialize<T>(line, _properties.SerializationOptions);
				}
			}
		}

		/// <summary>
		/// Ensures that the model used for this sector is compatible with the current embedding model.
		/// If not, rebuilds the sector with the current model.
		/// </summary>
		/// <returns>The task that represents the asynchronous operation.</returns>
		public async Task RebuildWithModelAsync()
		{
			if (_database.EmbeddingModel.Descriptor.FullName == _metadata.Model)
				return;

			var items = GetAll();
			Clear();
			await RecordRangeAsync(items);
		}

		/// <summary>
		/// Forces the replacement of the actual model with the current embedding model.
		/// </summary>
		/// <remarks>
		/// Useful for cases where the model name has changed but not the actual model itself, and no data needs to be rebuilt.
		/// </remarks>
		public void ForceActualizeModel()
		{
			if (_database.EmbeddingModel.Descriptor.FullName == _metadata.Model)
				return;

			_metadata.Model = _database.EmbeddingModel.Descriptor.FullName;
			SaveMetadata();
		}



		/// <summary>
		/// Retrieves the closest item to the query text.
		/// </summary>
		/// <param name="query">The text to search for.</param>
		/// <param name="transformQuery">Whether to apply the input transformer to the query.</param>
		/// <param name="cancellationToken">The cancellation token to use for this operation.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		public async Task<(T Item, float Score)> QueryAsync(string query, bool transformQuery = true, CancellationToken cancellationToken = default)
		{
			return (await QueryAsync(query, 1, transformQuery, cancellationToken))[0];
		}

		/// <summary>
		/// Retrieves the closest items to the query text.
		/// </summary>
		/// <param name="query">The text to search for.</param>
		/// <param name="maxCount">The maximum number of results to return.</param>
		/// <param name="transformQuery">Whether to transform the input text using the transformer.</param>
		/// <param name="cancellationToken">The cancellation token to use for this operation.</param>
		/// <returns>A task that represents the asynchronous operation. The results containing a tuple containing the similarity score and the item.</returns>
		public async Task<(T Item, float Score)[]> QueryAsync(string query, int maxCount, bool transformQuery = true, CancellationToken cancellationToken = default)
		{
#if MEASURE_TIME
			var timeStart = DateTime.Now;
#endif

			if (_metadata.Count == 0)
				throw new InvalidOperationException("No data recorded in this sector.");
			if (_database.EmbeddingModel.Descriptor.FullName != _metadata.Model)
				throw new InvalidOperationException("Embedding model has changed since the sector was updated. " +
					"Use RebuildWithModelAsync() to rebuild the sector with the new model " +
					"or forcefully change the model with ForceActualizeModel() method.");

			string input = transformQuery ? await _properties.InputTransformer.TransformAsync(query, cancellationToken) : query;

#if MEASURE_TIME
			var timeEndTransform = DateTime.Now;
			var timeTransform = timeEndTransform - timeStart;
#endif

			EmbeddingResult embeddingResult = await _database.EmbeddingModel.EmbedAsync(input, cancellationToken: cancellationToken);
			Embedding queryEmbedding = embeddingResult.Embedding;

#if MEASURE_TIME
			var timeEndEmbedding = DateTime.Now;
			var timeEmbeddings = timeEndEmbedding - timeEndTransform;
#endif

			if (_metadata.EmbeddingDimension != queryEmbedding.Dimensions)
				throw new InvalidOperationException($"Embedding dimension mismatch (expected {_metadata.EmbeddingDimension}, got {queryEmbedding.Dimensions}).");

			var dimensions = queryEmbedding.Dimensions;
			byte[] buffer = new byte[dimensions * 4];
			float[] embeddingBuffer = new float[dimensions];

			List<(int Index, float Score)> scores = new();

			lock (_lock)
			{
				if (!File.Exists(_vectorsPath))
					throw new InvalidOperationException("No vectors found in sector. Files may be corrupted or not initialized properly.");
				queryEmbedding = queryEmbedding.Normalized;

				int currentIndex = 0;
				using (var vecStream = new FileStream(_vectorsPath, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					while (vecStream.Position < vecStream.Length)
					{
						int readBytes = vecStream.Read(buffer, 0, buffer.Length);
						if (readBytes != buffer.Length)
							throw new InvalidOperationException("Unexpected number of bytes read from vectors file. Files may be corrupted.");

						Buffer.BlockCopy(buffer, 0, embeddingBuffer, 0, buffer.Length);
						var score = _properties.SimilarityFunction(embeddingBuffer.AsSpan(), queryEmbedding);
						scores.Add((currentIndex, score));

						currentIndex++;
					}
				}
			}

			var best = scores.OrderByDescending(s => s.Score).Take(maxCount);
			List<(T Item, float Score)> results = new();

			using var offsetStream = new FileStream(_dataOffsetsPath, FileMode.Open, FileAccess.Read, FileShare.Read);
			using var offsetReader = new BinaryReader(offsetStream);
			using var dataStream = new FileStream(_dataPath, FileMode.Open, FileAccess.Read, FileShare.Read);
			using var dataReader = new StreamReader(dataStream);

			foreach (var (index, score) in best)
			{
				offsetStream.Seek(index * sizeof(long), SeekOrigin.Begin);
				long offset = offsetReader.ReadInt64();

				dataStream.Seek(offset, SeekOrigin.Begin);
				string jsonLine = dataReader.ReadLine();

				T item = JsonSerializer.Deserialize<T>(jsonLine, _properties.SerializationOptions);
				results.Add((item, score));
			}

#if MEASURE_TIME
			var timeEnd = DateTime.Now;

			var timeTotal = timeEnd - timeStart;
			var nonEmbeddingTime = timeTotal - timeEmbeddings;

			Console.WriteLine($"Looked {Count} items in {timeTotal.TotalMilliseconds} ms. " +
				$"Transforming took {timeTransform.TotalMilliseconds} ms. " +
				$"Embedding took {timeEmbeddings.TotalMilliseconds} ms. " +
				$"Non-embedding took {nonEmbeddingTime.TotalMilliseconds} ms.");
#endif

			return results.ToArray();
		}
	}
}