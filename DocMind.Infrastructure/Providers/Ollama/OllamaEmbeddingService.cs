using DocMind.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using OllamaSharp;

namespace DocMind.Infrastructure.Providers.Ollama
{
    public class OllamaEmbeddingService : IEmbeddingService
    {
        private readonly OllamaApiClient _client;
        private const string ModelName = "all-minilm";

        public int Dimensions => 384;

        public OllamaEmbeddingService(OllamaApiClient client)
        {
            _client = client;
        }

        public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
        {
            var response = await _client.EmbedAsync(new OllamaSharp.Models.EmbedRequest
            {
                Model = ModelName,
                Input = new List<string> { text }
            }, cancellationToken);

            return response.Embeddings[0].Select(d => (float)d).ToArray();
        }

        public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
        {
            var response = await _client.EmbedAsync(new OllamaSharp.Models.EmbedRequest
            {
                Model = ModelName,
                Input = texts.ToList()
            }, cancellationToken);

            return response.Embeddings.Select(e => e.Select(d => (float)d).ToArray()).ToList();
        }
    }
}
