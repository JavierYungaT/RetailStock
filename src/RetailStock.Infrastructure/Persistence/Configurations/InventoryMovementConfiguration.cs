using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailStock.Domain.Entities;

namespace RetailStock.Infrastructure.Persistence.Configurations;

internal sealed class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("InventoryMovements", t => t.HasCheckConstraint("CK_InventoryMovements_Quantity", "[Quantity] > 0"));
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne(m => m.Product)
            .WithMany()
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Dos FKs hacia Warehouse: SQL Server no permite "múltiples rutas de cascada",
        // por eso ambas son Restrict (NO ACTION).
        builder.HasOne(m => m.FromWarehouse)
            .WithMany()
            .HasForeignKey(m => m.FromWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.ToWarehouse)
            .WithMany()
            .HasForeignKey(m => m.ToWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.User)
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Consultas típicas de kardex: movimientos de un producto ordenados por fecha
        builder.HasIndex(m => new { m.ProductId, m.Date });
    }
}
