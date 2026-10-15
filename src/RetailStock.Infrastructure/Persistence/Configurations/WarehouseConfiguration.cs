using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailStock.Domain.Entities;
using RetailStock.Infrastructure.Persistence.Seed;

namespace RetailStock.Infrastructure.Persistence.Configurations;

internal sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(w => w.Name).IsUnique();

        builder.Property(w => w.Location).HasMaxLength(200).IsRequired();

        builder.HasData(SeedData.Warehouses);
    }
}
