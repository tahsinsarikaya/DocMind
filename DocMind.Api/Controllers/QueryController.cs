using DocMind.Api.Contracts;
using DocMind.Api.Warmup;
using DocMind.Domain.Abstractions;
using DocMind.Domain.Services;
using Microsoft.AspNetCore.Mvc;

namespace DocMind.Api.Controllers
{
    [ApiController]
    [Route("query")]
    public class QueryController : ControllerBase
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorStore _vectorStore;
        private readonly IChatService _chatService;
        private readonly RagPromptBuilder _promptBuilder;
        private readonly WarmupState _warmupState;

        public QueryController(
            IEmbeddingService embeddingService,
            IVectorStore vectorStore,
            IChatService chatService,
            RagPromptBuilder promptBuilder,
            WarmupState warmupState)
        {
            _embeddingService = embeddingService;
            _vectorStore = vectorStore;
            _chatService = chatService;
            _promptBuilder = promptBuilder;
            _warmupState = warmupState;
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] AskQuestionRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Question))
                return BadRequest(new { error = "Question cannot be empty." });

            if (!_warmupState.IsReady)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "The chat model is still warming up. Try again in a moment." });

            var queryEmbedding = await _embeddingService.EmbedAsync(request.Question, cancellationToken);
            var results = await _vectorStore.SearchAsync(queryEmbedding, request.TopK, cancellationToken);

            if (results.Count == 0)
                return Ok(new { answer = "No relevant documents found.", sources = Array.Empty<object>() });

            var contextChunks = results.Select(r => r.Chunk).ToList();
            var systemPrompt = _promptBuilder.BuildSystemPrompt();
            var userMessage = _promptBuilder.BuildUserMessage(request.Question, contextChunks);

            var answer = await _chatService.CompleteAsync(systemPrompt, userMessage, cancellationToken);

            return Ok(new
            {
                answer,
                sources = results.Select(r => new { r.Chunk.DocumentId, r.Chunk.Position, r.Score })
            });
        }
    }
}
