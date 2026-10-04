using DocMind.Domain.Entities;
using DocMind.Infrastructure.VectorStore;
using DocMind.Tests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;


namespace DocMind.Tests
{
    public class QdrantVectorStoreTests : IClassFixture<QdrantFixture>
    {
        private readonly QdrantVectorStore _sut;

        public QdrantVectorStoreTests(QdrantFixture fixture)
        {
            _sut = new QdrantVectorStore(fixture.CreateClient());
        }

        private static float[] BuildVector(int primaryDimension, float magnitude = 5f, float secondaryNoise = 0f, int dimensions = 8)
        {
            var vector = new float[dimensions];
            vector[primaryDimension % dimensions] = magnitude;
            if (secondaryNoise != 0f)
                vector[(primaryDimension + 1) % dimensions] = secondaryNoise;
            return vector;
        }

        [Fact]
        public async Task UpsertAndSearch_ReturnsTheUpsertedChunk()
        {
            await _sut.EnsureCollectionExistsAsync(vectorSize: 8);

            var documentId = Guid.NewGuid();
            var vector = BuildVector(primaryDimension: 0);
            var chunk = new DocumentChunk(Guid.NewGuid(), documentId, 0, "Test content", vector);
            await _sut.UpsertAsync(chunk);

            var results = await _sut.SearchAsync(vector, topK: 1);

            Assert.Single(results);
            Assert.Equal(chunk.Content, results[0].Chunk.Content);
            Assert.Equal(documentId, results[0].Chunk.DocumentId);
        }

        [Fact]
        public async Task Search_RanksCloserVectorHigherThanFartherOne()
        {
            await _sut.EnsureCollectionExistsAsync(vectorSize: 8);

            var documentId = Guid.NewGuid();
            var query = BuildVector(primaryDimension: 1);
            // closeChunk: query ile aynı doğrultuda, ufak bir sapma var — yüksek cosine benzerliği
            var closeChunk = new DocumentChunk(Guid.NewGuid(), documentId, 0, "Close chunk", BuildVector(primaryDimension: 1, secondaryNoise: 0.1f));
            // farChunk: tamamen farklı bir boyutta (ortogonal) — cosine benzerliği ~0
            var farChunk = new DocumentChunk(Guid.NewGuid(), documentId, 1, "Far chunk", BuildVector(primaryDimension: 2));

            await _sut.UpsertAsync(closeChunk);
            await _sut.UpsertAsync(farChunk);

            var results = await _sut.SearchAsync(query, topK: 2);

            Assert.Equal(2, results.Count);
            Assert.Equal("Close chunk", results[0].Chunk.Content);
            Assert.True(results[0].Score > results[1].Score);
        }

        [Fact]
        public async Task DeleteByDocumentId_RemovesOnlyThatDocumentsChunks()
        {
            await _sut.EnsureCollectionExistsAsync(vectorSize: 8);

            var documentToDelete = Guid.NewGuid();
            var documentToKeep = Guid.NewGuid();
            var query = BuildVector(primaryDimension: 3);

            await _sut.UpsertAsync(new DocumentChunk(Guid.NewGuid(), documentToDelete, 0, "Delete me", query));
            await _sut.UpsertAsync(new DocumentChunk(Guid.NewGuid(), documentToKeep, 0, "Keep me", BuildVector(primaryDimension: 3, secondaryNoise: 0.1f)));

            await _sut.DeleteByDocumentIdAsync(documentToDelete);

            var results = await _sut.SearchAsync(query, topK: 10);

            Assert.Single(results);
            Assert.Equal("Keep me", results[0].Chunk.Content);
        }
    }
}
