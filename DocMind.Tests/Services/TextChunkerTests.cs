using DocMind.Domain.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocMind.Tests.Services
{
    public class TextChunkerTests
    {
        private readonly TextChunker _sut = new();

        [Theory]
        [InlineData("")]
        [InlineData("   \n  ")]
        public void Chunk_WithEmptyOrWhitespaceText_ReturnsNoChunks(string text)
        {
            var result = _sut.Chunk(text);

            Assert.Empty(result);
        }

        [Fact]
        public void Chunk_WithShortText_ReturnsSingleTrimmedChunk()
        {
            var result = _sut.Chunk("  Short text.  ");

            Assert.Single(result);
            Assert.Equal("Short text.", result[0]);
        }

        [Fact]
        public void Chunk_WithWindowsLineEndings_NormalizesThem()
        {
            var result = _sut.Chunk("Line one\r\nLine two");

            Assert.Single(result);
            Assert.DoesNotContain("\r", result[0]);
        }

        [Fact]
        public void Chunk_WithInvalidArguments_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.Chunk("text", maxChunkSize: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.Chunk("text", maxChunkSize: 10, overlap: 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => _sut.Chunk("text", maxChunkSize: 10, overlap: -1));
        }

        [Fact]
        public void Chunk_PrefersBreakingAtParagraphBoundary()
        {
            var paragraphA = "First paragraph sentence here.";
            var paragraphB = "Second paragraph continues on.";
            var text = paragraphA + "\n\n" + paragraphB;

            var result = _sut.Chunk(text, maxChunkSize: 50, overlap: 5);

            Assert.Equal(2, result.Count);
            Assert.Equal(paragraphA, result[0]);
            Assert.Contains(paragraphB, result[1]);
        }

        [Fact]
        public void Chunk_WithLongText_NeverExceedsMaxSize_AndKeepsEveryWordIntact()
        {
            var words = Enumerable.Range(0, 200).Select(i => $"w{i}").ToArray();
            var text = string.Join(" ", words);

            var result = _sut.Chunk(text, maxChunkSize: 50, overlap: 10);

            Assert.True(result.Count > 1);
            Assert.All(result, c => Assert.True(c.Length <= 50));

            // Örtüşme sayesinde hiçbir kelime iki parçanın sınırında yarım kalmamalı:
            // her kelime en az bir parçada bütün olarak bulunmalı.
            foreach (var word in words)
                Assert.Contains(result, c => c.Split(' ').Contains(word));
        }

        [Fact]
        public void Chunk_WithTextWithoutAnyWhitespace_FallsBackToHardCutAndTerminates()
        {
            var text = new string('x', 120);

            var result = _sut.Chunk(text, maxChunkSize: 50, overlap: 10);

            Assert.Equal(3, result.Count);
            Assert.All(result, c => Assert.True(c.Length <= 50));
        }
    }
}
