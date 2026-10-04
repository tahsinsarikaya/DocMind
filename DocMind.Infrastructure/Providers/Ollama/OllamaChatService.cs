using DocMind.Domain.Abstractions;
using OllamaSharp;
using OllamaSharp.Models.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocMind.Infrastructure.Providers.Ollama
{
    public class OllamaChatService : IChatService
    {
        private readonly OllamaApiClient _client;
        private const string ModelName = "llama3.2"; // "llama3.2:1b";

        public OllamaChatService(OllamaApiClient client)
        {
            _client = client;
        }

        public async Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
        {
            var messages = new List<Message>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userMessage)
        };

            var response = "";
            await foreach (var chunk in _client.ChatAsync(new ChatRequest
            {
                Model = ModelName,
                Messages = messages,
                Stream = false,
                KeepAlive = "30m" // model, son kullanımdan sonra 30 dakika bellekte kalsın
            }, cancellationToken))
            {
                response += chunk?.Message?.Content;
            }

            return response;
        }
    }
}
