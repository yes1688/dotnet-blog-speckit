using BlogSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlogSystem.Infrastructure.Data.Configurations
{
    /// <summary>
    /// BlogPost 實體的 EF Core 配置
    /// </summary>
    public class BlogPostConfiguration : IEntityTypeConfiguration<BlogPost>
    {
        public void Configure(EntityTypeBuilder<BlogPost> builder)
        {
            builder.ToTable("BlogPosts");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.Slug)
                .IsRequired()
                .HasMaxLength(300);

            builder.HasIndex(p => p.Slug)
                .IsUnique();

            builder.HasIndex(p => new { p.Status, p.PublishedAt });

            builder.HasIndex(p => p.CategoryId);

            builder.Property(p => p.Content)
                .IsRequired();

            builder.Property(p => p.Summary)
                .HasMaxLength(500);

            builder.Property(p => p.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(p => p.CreatedAt)
                .IsRequired();

            builder.Property(p => p.UpdatedAt)
                .IsRequired();

            builder.Property(p => p.ViewCount)
                .IsRequired()
                .HasDefaultValue(0);

            // 配置與 Category 的關聯 (多對一)
            builder.HasOne(p => p.Category)
                .WithMany(c => c.BlogPosts)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // 配置與 Tag 的多對多關聯
            builder.HasMany(p => p.Tags)
                .WithMany(t => t.BlogPosts)
                .UsingEntity(j => j.ToTable("BlogPostTags"));
        }
    }
}
