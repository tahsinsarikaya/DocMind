using DocMind.Api.Contracts;
using DocMind.Domain.Abstractions;
using DocMind.Domain.Entities;
using DocMind.Domain.Services;
using DocMind.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace DocMind.Api.Controllers
{
    [ApiController]
    [Route("documents")]
    public class DocumentsController : ControllerBase
    {
        private readonly DocMindDbContext _dbContext;
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorStore _vectorStore;
        private readonly TextChunker _chunker;

        public DocumentsController(
            DocMindDbContext dbContext,
            IEmbeddingService embeddingService,
            IVectorStore vectorStore,
            TextChunker chunker)
        {
            _dbContext = dbContext;
            _embeddingService = embeddingService;
            _vectorStore = vectorStore;
            _chunker = chunker;
        }

        [HttpPost]
        public async Task<IActionResult> Upload([FromBody] UploadDocumentRequest request, CancellationToken cancellationToken)
        {
            var document = new Document(Guid.NewGuid(), request.Title, request.Content);
            var chunkTexts = _chunker.Chunk(document.Content);

            if (chunkTexts.Count == 0)
                return BadRequest(new { error = "Document content produced no chunks." });

            var embeddings = await _embeddingService.EmbedBatchAsync(chunkTexts, cancellationToken);

            for (var i = 0; i < chunkTexts.Count; i++)
            {
                var chunk = new DocumentChunk(Guid.NewGuid(), document.Id, i, chunkTexts[i], embeddings[i]);
                document.AddChunk(chunk);
            }

            _dbContext.Documents.Add(document);
            await _dbContext.SaveChangesAsync(cancellationToken);

            foreach (var chunk in document.Chunks)
            {
                await _vectorStore.UpsertAsync(chunk, cancellationToken);
            }

            return CreatedAtAction(nameof(GetById), new { id = document.Id }, new { id = document.Id, chunkCount = document.Chunks.Count });
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var document = await _dbContext.Documents.FindAsync([id], cancellationToken);
            return document is null ? NotFound() : Ok(document);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var document = await _dbContext.Documents.FindAsync([id], cancellationToken);

            if (document is null)
                return NotFound();

            // Önce Qdrant'ı temizliyoruz. Sıra bilerek bu şekilde: PostgreSQL silme işlemi
            // Qdrant'tan önce başarısız olursa, doküman hâlâ DB'de durur ve tekrar
            // silinmeyi deneyebiliriz. Ama Qdrant'ı DB'den SONRA silseydik ve o adım
            // başarısız olsaydı, DB'de artık var olmayan bir doküman için Qdrant'ta
            // "hayalet" vektörler kalırdı — kurtarması daha zor bir tutarsızlık.
            await _vectorStore.DeleteByDocumentIdAsync(id, cancellationToken);

            _dbContext.Documents.Remove(document);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
    }
}
