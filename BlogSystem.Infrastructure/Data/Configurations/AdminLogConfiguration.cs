using BlogSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlogSystem.Infrastructure.Data.Configurations
{
    /// <summary>
    /// AdminLog 實體的 EF Core 配置
    /// </summary>
    public class AdminLogConfiguration : IEntityTypeConfiguration<AdminLog>
    {
        public void Configure(EntityTypeBuilder<AdminLog> builder)
        {
            builder.ToTable("AdminLogs");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Email)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(a => a.Action)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(a => a.Details)
                .HasMaxLength(1000);

            builder.Property(a => a.Timestamp)
                .IsRequired();

            builder.Property(a => a.IpAddress)
                .HasMaxLength(45);

            builder.HasIndex(a => new { a.Email, a.Timestamp });

            builder.HasIndex(a => a.Timestamp);
        }
    }
}
