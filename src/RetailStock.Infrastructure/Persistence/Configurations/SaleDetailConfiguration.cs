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

        // Índice para reportes de "productos más vendidos"
        builder.HasIndex(d => d.ProductId);
    }
}
