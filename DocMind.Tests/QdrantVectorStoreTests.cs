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

        private static float[] BuildVector(float seed, int dimensions = 8)
        {
            var vector = new float[dimensions];
            for (var i = 0; i < dimensions; i++)
                vector[i] = seed + i;
            return vector;
        }

        [Fact]
        public async Task UpsertAndSearch_ReturnsTheUpsertedChunk()
        {
            await _sut.EnsureCollectionExistsAsync(vectorSize: 8);

            var documentId = Guid.NewGuid();
            var chunk = new DocumentChunk(Guid.NewGuid(), documentId, 0, "Test content", BuildVector(1f));
            await _sut.UpsertAsync(chunk);

            var results = await _sut.SearchAsync(BuildVector(1f), topK: 1);

            Assert.Single(results);
            Assert.Equal(chunk.Content, results[0].Chunk.Content);
            Assert.Equal(documentId, results[0].Chunk.DocumentId);
        }

        [Fact]
        public async Task Search_RanksCloserVectorHigherThanFartherOne()
        {
            await _sut.EnsureCollectionExistsAsync(vectorSize: 8);

            var documentId = Guid.NewGuid();
            var closeChunk = new DocumentChunk(Guid.NewGuid(), documentId, 0, "Close chunk", BuildVector(1f));
            var farChunk = new DocumentChunk(Guid.NewGuid(), documentId, 1, "Far chunk", BuildVector(50f));

            await _sut.UpsertAsync(closeChunk);
            await _sut.UpsertAsync(farChunk);

            var results = await _sut.SearchAsync(BuildVector(1f), topK: 2);

            Assert.Equal(2, results.Count);
            Assert.Equal("Close chunk", results[0].Chunk.Content); // en yakın vektör ilk sırada olmalı
            Assert.True(results[0].Score > results[1].Score);
        }

        [Fact]
        public async Task DeleteByDocumentId_RemovesOnlyThatDocumentsChunks()
        {
            await _sut.EnsureCollectionExistsAsync(vectorSize: 8);

            var documentToDelete = Guid.NewGuid();
            var documentToKeep = Guid.NewGuid();

            await _sut.UpsertAsync(new DocumentChunk(Guid.NewGuid(), documentToDelete, 0, "Delete me", BuildVector(1f)));
            await _sut.UpsertAsync(new DocumentChunk(Guid.NewGuid(), documentToKeep, 0, "Keep me", BuildVector(2f)));

            await _sut.DeleteByDocumentIdAsync(documentToDelete);

            var results = await _sut.SearchAsync(BuildVector(1.5f), topK: 10);

            Assert.Single(results);
            Assert.Equal("Keep me", results[0].Chunk.Content);
        }
    }
}
