using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailStock.Domain.Entities;
using RetailStock.Infrastructure.Persistence.Seed;

namespace RetailStock.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeo de User con Fluent API. Cada entidad tiene su propia clase IEntityTypeConfiguration&lt;T&gt;.
/// Equivalente en JPA: las anotaciones @Table, @Column(length, unique), @Enumerated... pero fuera de la entidad.
/// </summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        // Un hash BCrypt mide 60 caracteres; se deja margen
        builder.Property(u => u.PasswordHash).HasMaxLength(100).IsRequired();

        // Enum guardado como texto: "Admin" en vez de 0 (como @Enumerated(EnumType.STRING))
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasData(SeedData.Users);
    }
}
