using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailStock.Domain.Entities;
using RetailStock.Infrastructure.Persistence.Seed;

namespace RetailStock.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(c => c.Name).IsUnique();

        builder.HasData(SeedData.Categories);
    }
}
