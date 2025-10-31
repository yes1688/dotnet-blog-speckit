using BlogSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlogSystem.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Tag 實體的 EF Core 配置
    /// </summary>
    public class TagConfiguration : IEntityTypeConfiguration<Tag>
    {
        public void Configure(EntityTypeBuilder<Tag> builder)
        {
            builder.ToTable("Tags");

            builder.HasKey(t => t.Id);

            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(30);

            builder.HasIndex(t => t.Name)
                .IsUnique();

            builder.Property(t => t.Slug)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(t => t.Slug)
                .IsUnique();

            builder.Property(t => t.UsageCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.HasIndex(t => t.UsageCount);
        }
    }
}
