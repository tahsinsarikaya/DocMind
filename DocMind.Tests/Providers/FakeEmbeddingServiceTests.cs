using DocMind.Infrastructure.Providers.Fake;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocMind.Tests.Providers
{
    public class FakeEmbeddingServiceTests
    {
        private readonly FakeEmbeddingService _sut = new();

        [Fact]
        public async Task EmbedAsync_WithSameText_ReturnsSameVector()
        {
            var first = await _sut.EmbedAsync("hello world");
            var second = await _sut.EmbedAsync("hello world");

            Assert.Equal(first, second);
        }

        [Fact]
        public async Task EmbedAsync_WithDifferentText_ReturnsDifferentVectors()
        {
            var first = await _sut.EmbedAsync("hello world");
            var second = await _sut.EmbedAsync("goodbye world");

            Assert.NotEqual(first, second);
        }

        [Fact]
        public async Task EmbedAsync_ReturnsVectorWithDeclaredDimensions()
        {
            var result = await _sut.EmbedAsync("test");

            Assert.Equal(_sut.Dimensions, result.Length);
        }

        [Fact]
        public async Task EmbedBatchAsync_ReturnsOneVectorPerInput_MatchingSingleEmbedResults()
        {
            var texts = new[] { "first", "second", "third" };

            var batchResults = await _sut.EmbedBatchAsync(texts);

            Assert.Equal(texts.Length, batchResults.Count);

            for (var i = 0; i < texts.Length; i++)
            {
                var individual = await _sut.EmbedAsync(texts[i]);
                Assert.Equal(individual, batchResults[i]);
            }
        }
    }
}
