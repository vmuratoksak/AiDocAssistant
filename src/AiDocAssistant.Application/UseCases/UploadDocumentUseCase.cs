using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Application.Interfaces;
using AiDocAssistant.Domain.Entities;
using Pgvector;

namespace AiDocAssistant.Application.UseCases
{
    public sealed class UploadDocumentUseCase
    {
        private readonly IDocumentParser _documentParser;
        private readonly ITextChunker _textChunker;
        private readonly IEmbeddingService _embeddingService;
        private readonly IDocumentRepository _documentRepository;

        public UploadDocumentUseCase(
            IDocumentParser documentParser,
            ITextChunker textChunker,
            IEmbeddingService embeddingService,
            IDocumentRepository documentRepository)
        {
            _documentParser = documentParser;
            _textChunker = textChunker;
            _embeddingService = embeddingService;
            _documentRepository = documentRepository;
        }

        public async Task<Guid> ExecuteAsync(
            string fileName,
            string contentType,
            Stream fileStream,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name is required.");
            if (fileStream == null || fileStream.Length == 0)
                throw new ArgumentException("File stream is empty.");

            // 1. Dokümandan metin çıkartma
            var text = await _documentParser.ExtractTextAsync(fileStream, cancellationToken);
            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("No text could be extracted from the document.");

            // 2. Metni üst üste binen chunk'lara ayırma (max 1000 karakter, 200 overlap)
            var chunks = _textChunker.Split(text, maxChunkSize: 1000, overlapSize: 200);

            // 3. Document entity'sini oluşturma
            var document = new Document(fileName, contentType);

            // 4. Her chunk için embedding üretip document'a ekleme
            for (int i = 0; i < chunks.Count; i++)
            {
                var chunkContent = chunks[i];
                var embedding = await _embeddingService.GenerateEmbeddingAsync(chunkContent, cancellationToken);
                document.AddChunk(chunkContent, order: i + 1, embedding: new Vector(embedding));
            }

            // 5. Veritabanına kaydetme
            await _documentRepository.AddAsync(document, cancellationToken);
            await _documentRepository.SaveChangesAsync(cancellationToken);

            return document.Id;
        }
    }
}
