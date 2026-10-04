using DocMind.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocMind.Infrastructure.Persistence
{
    public class DocMindDbContext : DbContext
    {
        public DocMindDbContext(DbContextOptions<DocMindDbContext> options) : base(options) { }

        public DbSet<Document> Documents => Set<Document>();
        public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocMindDbContext).Assembly);
        }
    }
}
