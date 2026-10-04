using System;
using System.Collections.Generic;
using System.Text;

namespace DocMind.Domain.Abstractions
{
    public interface IChatService
    {
        Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);
    }
}
