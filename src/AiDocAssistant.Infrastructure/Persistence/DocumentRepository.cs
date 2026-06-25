using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AiDocAssistant.Application.Interfaces;
using AiDocAssistant.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiDocAssistant.Infrastructure.Persistence
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly AppDbContext _context;

        public DocumentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.Documents
                .Include(d => d.Chunks)
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _context.Documents
                .Include(d => d.Chunks)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Document document, CancellationToken cancellationToken)
        {
            await _context.Documents.AddAsync(document, cancellationToken);
        }

        public Task DeleteAsync(Document document, CancellationToken cancellationToken)
        {
            _context.Documents.Remove(document);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
