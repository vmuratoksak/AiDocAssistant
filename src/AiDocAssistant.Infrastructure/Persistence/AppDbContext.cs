using Microsoft.EntityFrameworkCore;
using AiDocAssistant.Domain.Entities;

namespace AiDocAssistant.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentChunk> DocumentChunks { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            ChangeTracker.Tracked += OnEntityTracked;
        }

        private void OnEntityTracked(object sender, Microsoft.EntityFrameworkCore.ChangeTracking.EntityTrackedEventArgs e)
        {
            if (!e.FromQuery && e.Entry.Entity is DocumentChunk && e.Entry.State == EntityState.Modified)
            {
                e.Entry.State = EntityState.Added;
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // pgvector eklentisini veritabanında aktif hale getiriyoruz
            modelBuilder.HasPostgresExtension("vector");

            // Document yapılandırması
            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.ContentType).HasMaxLength(100);
                entity.Property(e => e.UploadedAt).IsRequired();
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ErrorMessage).HasMaxLength(1000);

                // Document -> DocumentChunk ilişkisi (Cascade Delete)
                entity.HasMany(e => e.Chunks)
                      .WithOne()
                      .HasForeignKey(c => c.DocumentId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // DocumentChunk yapılandırması
            modelBuilder.Entity<DocumentChunk>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.Order).IsRequired();

                // Embedding alanını pgvector'ın vector tipine eşliyoruz
                // Sabit boyut yerine genel "vector" tipi esneklik sağlar
                entity.Property(e => e.Embedding)
                      .HasColumnType("vector");
            });
        }
    }
}
