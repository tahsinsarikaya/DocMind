using DocMind.Domain.Abstractions;
using DocMind.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocMind.Domain.Services
{
    public class RagPromptBuilder
    {
        private const string SystemPrompt =
            "You are a document question-answering assistant. Answer the user's question " +
            "using ONLY the information in the provided context. If the context does not " +
            "contain enough information to answer, say so explicitly instead of guessing " +
            "or using outside knowledge. Cite which context section(s) you used.";

        public string BuildSystemPrompt() => SystemPrompt;

        public string BuildUserMessage(string question, IReadOnlyList<RetrievedChunk> contextChunks)
        {
            if (string.IsNullOrWhiteSpace(question))
                throw new ArgumentException("Question cannot be empty.", nameof(question));

            if (contextChunks.Count == 0)
                throw new ArgumentException("At least one context chunk is required.", nameof(contextChunks));

            var sb = new StringBuilder();
            sb.AppendLine("Context:");

            for (var i = 0; i < contextChunks.Count; i++)
            {
                sb.AppendLine($"--- Section {i + 1} ---");
                sb.AppendLine(contextChunks[i].Content);
                sb.AppendLine();
            }

            sb.AppendLine("Question:");
            sb.AppendLine(question);

            return sb.ToString();
        }
    }
}
