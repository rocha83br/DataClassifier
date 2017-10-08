using System;
using SQLite.CodeFirst;
using System.Data.Common;
using System.Data.Entity;
using Rochas.KnowledgeBaseServer.Models;

namespace Rochas.KnowledgeBaseServer.Repositories
{
    public class KnowledgeContext : DbContext, IDisposable
    {
        public DbSet<KnowledgeGroup> Knowledgement { get; set; }
        public DbSet<KnowledgeHash> KnowledgementData { get; set; }

        public KnowledgeContext(DbConnection connection, bool lazyLoading = false) : base(connection, false)
        {
            base.Configuration.LazyLoadingEnabled = lazyLoading;
            base.Configuration.ValidateOnSaveEnabled = true;

            base.Database.CreateIfNotExists();
            base.Database.Initialize(false);
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            var sqliteConnectionInitializer = new SqliteCreateDatabaseIfNotExists<KnowledgeContext>(modelBuilder);
            Database.SetInitializer(sqliteConnectionInitializer);
        }
    }
}