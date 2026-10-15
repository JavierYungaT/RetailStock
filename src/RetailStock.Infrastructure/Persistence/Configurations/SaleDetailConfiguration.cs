using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailStock.Domain.Entities;

namespace RetailStock.Infrastructure.Persistence.Configurations;

internal sealed class SaleDetailConfiguration : IEntityTypeConfiguration<SaleDetail>
{
    public void Configure(EntityTypeBuilder<SaleDetail> builder)
    {
        builder.ToTable("SaleDetails", t => t.HasCheckConstraint("CK_SaleDetails_Quantity", "[Quantity] > 0"));
        builder.HasKey(d => d.Id);

        builder.Property(d => d.UnitPrice).HasPrecision(18, 2);
        builder.Property(d => d.LineTotal).HasPrecision(18, 2);

        builder.HasOne(d => d.Product)
            .WithMany()
            .HasForeignKey(d => d.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Consultas por producto (historial de ventas de un producto)
        builder.HasIndex(d => d.ProductId);

        // Índice de la FK SaleId (EF lo crea por convención) ampliado para sp_TopProducts:
        // tras filtrar Sales por fecha, el JOIN busca los detalles por SaleId (Seek) y obtiene
        // ProductId, Quantity y LineTotal de la hoja del índice sin tocar la tabla.
        builder.HasIndex(d => d.SaleId)
            .IncludeProperties(d => new { d.ProductId, d.Quantity, d.LineTotal });
    }
}
