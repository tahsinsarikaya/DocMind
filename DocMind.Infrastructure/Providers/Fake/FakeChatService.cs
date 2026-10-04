using DocMind.Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocMind.Infrastructure.Providers.Fake
{
    public class FakeChatService : IChatService
    {
        public Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
        {
            // Gerçek bir LLM çağrısı yok — sadece akışın uçtan uca çalıştığını
            // (soru alındı, bağlam bulundu, "bir cevap" üretildi) doğrulamak için.
            return Task.FromResult($"[FAKE RESPONSE] Based on the provided context, here is a placeholder answer to your question.");
        }
    }
}
