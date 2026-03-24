using Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Data.Configurations;

public class TodoConfiguration : IEntityTypeConfiguration<Todo>
{
    public void Configure(EntityTypeBuilder<Todo> builder)
    {
        builder.HasKey(t => t.Id);
        builder.HasIndex(t => t.UserId);
        builder.Property(t => t.Priority).HasConversion<string>();
    }
}
