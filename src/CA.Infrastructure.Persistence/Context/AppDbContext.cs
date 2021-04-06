﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CA.Core.Application.Contracts.Interfaces;
using CA.Core.Domain.Persistence.Common;
using CA.Core.Domain.Persistence.Entities;
using CA.Core.Domain.Persistence.Enums;
using CA.Infrastructure.Persistence.Helpers;
using LinqToDB.Data;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CA.Infrastructure.Persistence.Context
{
    public class AppDbContext : DbContext
    {
        private readonly IDateTimeService _dateTime;
        private readonly IAuthenticatedUser _authenticatedUser;

        /// <summary>
        ///     Linq2Db instance of DbContext. Use it for bulk insert and bulk fetch. 
        /// </summary>
        public DataConnection Linq2Db { get; }

        public AppDbContext(DbContextOptions<AppDbContext> options, IDateTimeService dateTime, IAuthenticatedUser authenticatedUser)
            : base(options)
        {
            _dateTime = dateTime;
            _authenticatedUser = authenticatedUser;
            Linq2Db = options.CreateLinqToDbConnection();
        }

        public DbSet<Audit> Audits { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostCategory> PostCategories { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<PostTag> PostTags { get; set; }
       

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>(options =>
            {
                options.HasIndex(x => x.Id);
                options.HasOne(x => x.ParentCategory)
                    .WithMany(y => y.ChildCategories)
                    .HasForeignKey(x => x.ParentId);
                options.HasIndex(x => x.Slug)
                    .IsUnique();
            });

            modelBuilder.Entity<Post>(options =>
            {
                options.HasKey(x => x.Id);
                options.HasOne(x => x.ParentPost)
                    .WithMany(y => y.ChildPosts)
                    .HasForeignKey(x => x.ParentId);
                options.Property(x => x.Content).HasColumnType("NVARCHAR(MAX)");
                options.HasIndex(x => x.Slug)
                    .IsUnique();
            });

            modelBuilder.Entity<PostCategory>(options =>
            {
                options.HasKey(bc => new {bc.PostId, bc.CategoryId});
                options.HasOne(bc => bc.Post)
                    .WithMany(y => y.Categories)
                    .HasForeignKey(bc => bc.PostId);
                options.HasOne(x => x.Category)
                    .WithMany(y => y.Posts)
                    .HasForeignKey(x => x.CategoryId);
            });

            modelBuilder.Entity<Comment>(options =>
            {
                options.HasKey(x => x.Id);
                options.HasOne(x => x.Post)
                    .WithMany(y => y.Comments)
                    .HasForeignKey(x => x.PostId);
            });

            modelBuilder.Entity<Tag>(options =>
            {
                options.HasKey(x => x.Id);
                options.HasOne(x => x.ParentTag)
                    .WithMany(y => y.ChildTags)
                    .HasForeignKey(x => x.ParentId);
                options.HasIndex(x => x.Slug)
                    .IsUnique();
            });

            modelBuilder.Entity<PostTag>(options =>
            {
                options.HasKey(x => new {x.PostId, x.TagId});
                options.HasOne(x => x.Post)
                    .WithMany(y => y.Tags)
                    .HasForeignKey(x => x.PostId);
                options.HasOne(x => x.Tag)
                    .WithMany(y => y.Posts)
                    .HasForeignKey(x => x.TagId);
            });
           
            base.OnModelCreating(modelBuilder);
        }

        public async Task<int> SaveChangesAsync()
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>().ToList())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreationDate = _dateTime.NowUtc;
                        entry.Entity.CreatedBy = _authenticatedUser.UserId;
                        break;

                    case EntityState.Modified:
                        entry.Entity.LastUpdatedDate = _dateTime.NowUtc;
                        entry.Entity.LastUpdatedBy = _authenticatedUser.UserId;
                        break;
                }
            }

            if (_authenticatedUser.UserId != null) await AuditLogging();
            return await base.SaveChangesAsync();
        }

        public async Task AuditLogging()
        {
            ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditEntry>();
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is Audit || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;
                var auditEntry = new AuditEntry(entry)
                {
                    TableName = entry.Entity.GetType().Name,
                    UserId = _authenticatedUser.UserId,
                    UserName = _authenticatedUser.UserName
                };
                auditEntries.Add(auditEntry);
                foreach (var property in entry.Properties)
                {
                    var propertyName = property.Metadata.Name;
                    if (property.Metadata.IsPrimaryKey())
                    {
                        auditEntry.KeyValues[propertyName] = property.CurrentValue;
                        continue;
                    }
                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditEntry.AuditType = AuditType.Create;
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                            break;
                        case EntityState.Deleted:
                            auditEntry.AuditType = AuditType.Delete;
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            break;
                        case EntityState.Modified:
                            if (property.IsModified)
                            {
                                auditEntry.ChangedColumns.Add(propertyName);
                                auditEntry.AuditType = AuditType.Update;
                                auditEntry.OldValues[propertyName] = property.OriginalValue;
                                auditEntry.NewValues[propertyName] = property.CurrentValue;
                            }
                            break;
                    }
                }
            }
            foreach (var auditEntry in auditEntries)
            {
                await Audits.AddAsync(auditEntry.ToAudit());
            }
        }
    }
}
