using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailStock.Domain.Entities;
using RetailStock.Infrastructure.Persistence.Seed;

namespace RetailStock.Infrastructure.Persistence.Configurations;

internal sealed class StockConfiguration : IEntityTypeConfiguration<Stock>
{
    public void Configure(EntityTypeBuilder<Stock> builder)
    {
        // CHECK constraint: segunda línea de defensa en la BD por si algo saltara la regla del dominio
        builder.ToTable("Stocks", t => t.HasCheckConstraint("CK_Stocks_Quantity", "[Quantity] >= 0"));

        // Clave primaria compuesta (como @IdClass o @EmbeddedId en JPA)
        builder.HasKey(s => new { s.ProductId, s.WarehouseId });

        // rowversion: SQL Server lo cambia en cada UPDATE; EF lo agrega al WHERE (concurrencia optimista)
        builder.Property(s => s.RowVersion).IsRowVersion();

        builder.HasOne(s => s.Product)
            .WithMany(p => p.Stocks)
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Warehouse)
            .WithMany(w => w.Stocks)
            .HasForeignKey(s => s.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índice para consultar "todo el stock de una bodega" (la PK ya cubre las búsquedas por ProductId)
        builder.HasIndex(s => s.WarehouseId);

        builder.HasData(SeedData.Stocks);
    }
}
