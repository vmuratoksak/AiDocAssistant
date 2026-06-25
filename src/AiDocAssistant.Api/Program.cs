using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using AiDocAssistant.Infrastructure.Persistence;
using AiDocAssistant.Infrastructure.AI;
using AiDocAssistant.Infrastructure.Parser;
using AiDocAssistant.Infrastructure.VectorSearch;
using AiDocAssistant.Infrastructure.Prompts;
using AiDocAssistant.Infrastructure.BackgroundJobs;
using AiDocAssistant.Infrastructure.FileStorage;
using AiDocAssistant.Api.BackgroundJobs;
using AiDocAssistant.Application.Interfaces;
using AiDocAssistant.Application.UseCases;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

// Controller'ları ve Swagger'ı ekle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS Politikası Tanımla (Arayüzün bağımsız portlardan bağlanabilmesi için)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Veritabanı sağlayıcı seçimi (PostgreSQL veya Mock)
var dbSettings = builder.Configuration.GetSection("DatabaseSettings");
var dbProvider = dbSettings.GetValue<string>("Provider") ?? "Mock";

// PostgreSQL + pgvector DbContext Yapılandırması (EF Core tasarım zamanı araçları için her zaman kayıtlı olmalıdır)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, x => x.UseVector()));

if (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
    builder.Services.AddScoped<IVectorStore, VectorStore>();
}
else
{
    // PostgreSQL ve pgvector kurulu olmayan durumlar için Bellek İçi (Mock) DB
    builder.Services.AddSingleton<IDocumentRepository, InMemoryDocumentRepository>();
    builder.Services.AddSingleton<IVectorStore, InMemoryVectorStore>();
}

// Alt Yapı Servis Kayıtları
builder.Services.AddSingleton<ITextChunker, TextChunker>();
builder.Services.AddSingleton<IDocumentParser, DocumentParser>();
builder.Services.AddSingleton<IPromptProvider, FilePromptProvider>();
builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();

// Kuyruk ve Arka Plan Worker Servis Kayıtları
builder.Services.AddSingleton<IDocumentQueue, ChannelQueue>();
builder.Services.AddHostedService<DocumentProcessingWorker>();

builder.Services.AddScoped<IAiChatService, AiChatService>();
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();

// Use-Case Kayıtları
builder.Services.AddScoped<UploadDocumentUseCase>();
builder.Services.AddScoped<AskDocumentQuestionUseCase>();

// AI Provider Yapılandırması (Ollama veya Mock)
var aiSettings = builder.Configuration.GetSection("AiSettings");
var provider = aiSettings.GetValue<string>("Provider") ?? "Mock";
var ollamaUrl = aiSettings.GetValue<string>("OllamaUrl") ?? "http://localhost:11434";
var chatModel = aiSettings.GetValue<string>("ChatModel") ?? "llama3";
var embeddingModel = aiSettings.GetValue<string>("EmbeddingModel") ?? "nomic-embed-text";

if (provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
{
    // Ollama Entegrasyonu (Microsoft.Extensions.AI.Ollama üzerinden)
    builder.Services.AddSingleton<IChatClient>(sp => 
        new OllamaChatClient(new Uri(ollamaUrl), chatModel));
        
    builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp => 
        new OllamaEmbeddingGenerator(new Uri(ollamaUrl), embeddingModel));
}
else
{
    // Hızlı test ve internetsiz/kurulumsuz çalışma için Mock AI
    builder.Services.AddSingleton<IChatClient>(sp => new MockChatClient());
    builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp => new MockEmbeddingGenerator());
}

var app = builder.Build();

// Veritabanı migrasyonlarını başlangıçta otomatik uygula (Sadece PostgreSQL seçildiğinde)
if (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            if (context.Database.IsRelational())
            {
                context.Database.Migrate();
            }
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Veritabanı migrasyonu sırasında bir hata oluştu.");
        }
    }
}

// HTTP İstek Hattı Yapılandırması
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();
app.MapControllers();

app.Run();
