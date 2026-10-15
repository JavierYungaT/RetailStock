namespace RetailStock.Tests.TestHelpers;

/// <summary>
/// Las entidades tienen "Id { get; private set; }": en producción el Id lo asigna la BD (IDENTITY).
/// En pruebas unitarias sin BD lo asignamos por reflexión para poder armar escenarios con varios productos.
/// Equivalente en Java: ReflectionTestUtils.setField(product, "id", 1) de Spring Test.
/// </summary>
public static class EntityTestExtensions
{
    public static T WithId<T>(this T entity, int id) where T : class
    {
        var property = typeof(T).GetProperty("Id")
                       ?? throw new InvalidOperationException($"{typeof(T).Name} no tiene propiedad Id.");
        property.SetValue(entity, id);
        return entity;
    }
}
