using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailStock.Domain.Entities;
using RetailStock.Infrastructure.Persistence.Seed;

namespace RetailStock.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code).HasMaxLength(30).IsRequired();
        // Índice único: no puede haber dos productos con el mismo código (como @Column(unique = true))
        builder.HasIndex(p => p.Code).IsUnique();

        builder.Property(p => p.Name).HasMaxLength(150).IsRequired();

        // decimal(18,2): sin esto EF avisa y SQL Server truncaría a su precisión por defecto
        builder.Property(p => p.Price).HasPrecision(18, 2);

        // N:1 con Category. Restrict: no se puede borrar una categoría que tiene productos
        // (equivale a @ManyToOne sin cascade REMOVE)
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(SeedData.Products);
    }
}
