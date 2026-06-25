using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using AiDocAssistant.Infrastructure.Persistence;
using AiDocAssistant.Infrastructure.AI;
using AiDocAssistant.Infrastructure.Parser;
using AiDocAssistant.Infrastructure.VectorSearch;
using AiDocAssistant.Infrastructure.Prompts;
using AiDocAssistant.Infrastructure.FileStorage;
using AiDocAssistant.Application.Interfaces;
using Microsoft.Extensions.AI;
using AiDocAssistant.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Veritabanı sağlayıcı seçimi (PostgreSQL veya Mock)
var dbSettings = builder.Configuration.GetSection("DatabaseSettings");
var dbProvider = dbSettings.GetValue<string>("Provider") ?? "Mock";

// PostgreSQL + pgvector DbContext Yapılandırması
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

builder.Services.AddScoped<IAiChatService, AiChatService>();
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();

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

// Worker Servisi Kaydı
builder.Services.AddHostedService<DocumentProcessingWorker>();

var host = builder.Build();
host.Run();
