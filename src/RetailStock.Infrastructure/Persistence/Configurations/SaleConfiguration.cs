using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailStock.Domain.Entities;

namespace RetailStock.Infrastructure.Persistence.Configurations;

internal sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");
        builder.HasKey(s => s.Id);

        // Formato de factura ecuatoriana: 001-001-000000123 (17 caracteres)
        builder.Property(s => s.Number).HasMaxLength(20).IsRequired();
        builder.HasIndex(s => s.Number).IsUnique();

        builder.Property(s => s.CustomerName).HasMaxLength(150).IsRequired();
        // Cédula (10) o RUC (13)
        builder.Property(s => s.CustomerId).HasMaxLength(13).IsRequired();

        builder.Property(s => s.Subtotal).HasPrecision(18, 2);
        builder.Property(s => s.Tax).HasPrecision(18, 2);
        builder.Property(s => s.Total).HasPrecision(18, 2);

        builder.Property(s => s.SyncedToSap).HasDefaultValue(false);

        // Índice para el reporte de ventas por día (Fase 6)
        builder.HasIndex(s => s.Date);

        builder.HasOne(s => s.Warehouse)
            .WithMany()
            .HasForeignKey(s => s.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1:N con SaleDetail. Cascade: si se borra la venta, se borran sus líneas
        // (como @OneToMany(cascade = ALL, orphanRemoval = true))
        builder.HasMany(s => s.Details)
            .WithOne(d => d.Sale)
            .HasForeignKey(d => d.SaleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
