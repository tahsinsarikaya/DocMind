using DocMind.Domain.Abstractions;
using DocMind.Domain.Entities;
using DocMind.Domain.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocMind.Tests.Services
{
    public class RagPromptBuilderTests
    {
        private readonly RagPromptBuilder _sut = new();

        private static RetrievedChunk BuildChunk(string content, int position = 0)
        {
            return new RetrievedChunk(Guid.NewGuid(), Guid.NewGuid(), position, content);
        }

        [Fact]
        public void BuildSystemPrompt_MentionsUsingOnlyProvidedContext()
        {
            var result = _sut.BuildSystemPrompt();

            Assert.Contains("ONLY", result);
        }

        [Fact]
        public void BuildUserMessage_IncludesQuestionAndAllChunkContents()
        {
            var chunks = new[] { BuildChunk("Paris is the capital of France."), BuildChunk("It has a population of over 2 million.", 1) };

            var result = _sut.BuildUserMessage("What is the capital of France?", chunks);

            Assert.Contains("What is the capital of France?", result);
            Assert.Contains("Paris is the capital of France.", result);
            Assert.Contains("It has a population of over 2 million.", result);
        }

        [Fact]
        public void BuildUserMessage_NumbersSectionsInOrder()
        {
            var chunks = new[] { BuildChunk("First chunk"), BuildChunk("Second chunk", 1) };

            var result = _sut.BuildUserMessage("Question", chunks);

            var firstIndex = result.IndexOf("Section 1", StringComparison.Ordinal);
            var secondIndex = result.IndexOf("Section 2", StringComparison.Ordinal);

            Assert.True(firstIndex >= 0 && secondIndex > firstIndex);
        }

        [Fact]
        public void BuildUserMessage_WithEmptyQuestion_Throws()
        {
            var chunks = new[] { BuildChunk("Some content") };

            Assert.Throws<ArgumentException>(() => _sut.BuildUserMessage("", chunks));
        }

        [Fact]
        public void BuildUserMessage_WithNoChunks_Throws()
        {
            Assert.Throws<ArgumentException>(() => _sut.BuildUserMessage("Question", Array.Empty<RetrievedChunk>()));
        }
    }
}
