using DocMind.Api.Middleware;
using DocMind.Api.Warmup;
using DocMind.Domain.Abstractions;
using DocMind.Domain.Services;
using DocMind.Infrastructure.Persistence;
using DocMind.Infrastructure.Providers.Fake;
using DocMind.Infrastructure.Providers.Ollama;
using DocMind.Infrastructure.VectorStore;
using Microsoft.EntityFrameworkCore;
using OllamaSharp;
using Qdrant.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<DocMindDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DocMindDb")));

builder.Services.AddSingleton(sp => new OllamaApiClient(new Uri(builder.Configuration["Ollama:Host"] ?? "http://localhost:11434")));

// Embedding sağlayıcısı seçimi
var embeddingProvider = builder.Configuration["Providers:Embedding"] ?? "Fake";
switch (embeddingProvider)
{
    case "Fake":
        builder.Services.AddSingleton<IEmbeddingService, FakeEmbeddingService>();
        break;
    case "Ollama":
        builder.Services.AddSingleton<IEmbeddingService, OllamaEmbeddingService>();
        break;
    default:
        throw new InvalidOperationException($"Unknown embedding provider: '{embeddingProvider}'. Supported: Fake, Ollama.");
}

// Chat sağlayıcısı seçimi
var chatProvider = builder.Configuration["Providers:Chat"] ?? "Fake";
switch (chatProvider)
{
    case "Fake":
        builder.Services.AddSingleton<IChatService, FakeChatService>();
        break;
    case "Ollama":
        builder.Services.AddSingleton<IChatService, OllamaChatService>();
        break;
    default:
        throw new InvalidOperationException($"Unknown chat provider: '{chatProvider}'. Supported: Fake, Ollama.");
}

// Qdrant bağlantısı
builder.Services.AddSingleton(sp => new QdrantClient(
    builder.Configuration["Qdrant:Host"]!,
    int.Parse(builder.Configuration["Qdrant:Port"]!)));

builder.Services.AddScoped<IVectorStore, QdrantVectorStore>();
builder.Services.AddScoped<QdrantVectorStore>(); // EnsureCollectionExistsAsync'e erişim için

builder.Services.AddSingleton<TextChunker>();
builder.Services.AddSingleton<RagPromptBuilder>();

builder.Services.AddSingleton<WarmupState>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();

// Qdrant koleksiyonunu doğru boyutta hazırla
using (var scope = app.Services.CreateScope())
{
    var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
    var vectorStore = scope.ServiceProvider.GetRequiredService<QdrantVectorStore>();
    await vectorStore.EnsureCollectionExistsAsync(embeddingService.Dimensions);
}

// Ollama kullanılıyorsa, modeli önceden belleğe yükle (cold-start gecikmesini önceden öde)
var warmupState = app.Services.GetRequiredService<WarmupState>();

if (chatProvider == "Ollama")
{
    _ = Task.Run(async () =>
    {
        using var warmupScope = app.Services.CreateScope();
        var chatService = warmupScope.ServiceProvider.GetRequiredService<IChatService>();
        await chatService.CompleteAsync("You are a helpful assistant.", "Say hi.", CancellationToken.None);
        warmupState.MarkReady();
    });
}
else
{
    warmupState.MarkReady(); // Fake sağlayıcıda ısınmaya gerek yok, direkt hazır
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "DocMind.Api" }));

app.MapGet("/health/ready", (WarmupState state) =>
    state.IsReady
        ? Results.Ok(new { ready = true })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();