using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailStock.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Migración escrita a mano (creada vacía con "dotnet ef migrations add"): EF no modela procedimientos
    /// almacenados, así que se versionan aquí con migrationBuilder.Sql(). Viajan con el resto del esquema
    /// y se aplican con "dotnet ef database update". Equivalente en Spring: un script V5__procedures.sql de Flyway.
    ///
    /// Reglas comunes de los SP:
    ///  - CREATE OR ALTER: idempotente (se puede volver a ejecutar sin error).
    ///  - SET NOCOUNT ON: evita los mensajes "(n rows affected)" que el cliente no necesita.
    ///  - Filtro por RANGO sobre Sales.Date con variables del MISMO tipo (datetime2): permite Index Seek
    ///    sobre IX_Sales_Date. Nunca CAST/YEAR(Date) en el WHERE (ver database/scripts/performance-demo.sql).
    ///  - @UtcOffsetMinutes: las fechas se guardan en UTC; el reporte agrupa por día LOCAL del negocio
    ///    (Ecuador = -300). Solo se aplica en el SELECT/GROUP BY y al calcular los límites del rango.
    /// </summary>
    public partial class AddReportStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cada llamada a Sql() se ejecuta como un lote separado: CREATE PROCEDURE debe ser la única sentencia del lote
            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.sp_SalesByDay
                    @From             DATE,
                    @To               DATE,
                    @UtcOffsetMinutes INT = 0
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @From > @To
                        THROW 50001, '@From no puede ser mayor que @To.', 1;

                    -- Límites del rango en UTC: [inicio del día @From local, inicio del día siguiente a @To local)
                    DECLARE @FromUtc DATETIME2 = DATEADD(MINUTE, -@UtcOffsetMinutes, CAST(@From AS DATETIME2));
                    DECLARE @ToUtc   DATETIME2 = DATEADD(MINUTE, -@UtcOffsetMinutes, CAST(DATEADD(DAY, 1, @To) AS DATETIME2));

                    SELECT
                        CAST(DATEADD(MINUTE, @UtcOffsetMinutes, s.[Date]) AS DATE) AS [Day],
                        COUNT(*)                                                    AS SalesCount,
                        SUM(s.Total)                                                AS Total,
                        CAST(AVG(s.Total) AS DECIMAL(18, 2))                        AS AverageTicket
                    FROM dbo.Sales AS s
                    WHERE s.[Date] >= @FromUtc
                      AND s.[Date] <  @ToUtc
                    GROUP BY CAST(DATEADD(MINUTE, @UtcOffsetMinutes, s.[Date]) AS DATE)
                    ORDER BY [Day];
                END
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.sp_LowStock
                    @Threshold INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- Productos ACTIVOS cuyo stock en alguna bodega está por debajo del umbral
                    SELECT
                        st.ProductId,
                        p.Code       AS ProductCode,
                        p.Name       AS ProductName,
                        c.Name       AS CategoryName,
                        st.WarehouseId,
                        w.Name       AS WarehouseName,
                        st.Quantity
                    FROM dbo.Stocks AS st
                    INNER JOIN dbo.Products   AS p ON p.Id = st.ProductId
                    INNER JOIN dbo.Categories AS c ON c.Id = p.CategoryId
                    INNER JOIN dbo.Warehouses AS w ON w.Id = st.WarehouseId
                    WHERE st.Quantity < @Threshold
                      AND p.IsActive = 1
                    ORDER BY st.Quantity, w.Name, p.Name;
                END
                """);

            migrationBuilder.Sql("""
                CREATE OR ALTER PROCEDURE dbo.sp_TopProducts
                    @From             DATE,
                    @To               DATE,
                    @Top              INT = 10,
                    @UtcOffsetMinutes INT = 0
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @From > @To
                        THROW 50001, '@From no puede ser mayor que @To.', 1;

                    DECLARE @FromUtc DATETIME2 = DATEADD(MINUTE, -@UtcOffsetMinutes, CAST(@From AS DATETIME2));
                    DECLARE @ToUtc   DATETIME2 = DATEADD(MINUTE, -@UtcOffsetMinutes, CAST(DATEADD(DAY, 1, @To) AS DATETIME2));

                    -- Revenue = suma de LineTotal (sin IVA). Ranking por cantidad y, en empate, por ingresos.
                    SELECT TOP (@Top)
                        d.ProductId,
                        p.Code                  AS ProductCode,
                        p.Name                  AS ProductName,
                        SUM(d.Quantity)         AS QuantitySold,
                        SUM(d.LineTotal)        AS Revenue,
                        COUNT(DISTINCT d.SaleId) AS SalesCount
                    FROM dbo.Sales AS s
                    INNER JOIN dbo.SaleDetails AS d ON d.SaleId = s.Id
                    INNER JOIN dbo.Products    AS p ON p.Id = d.ProductId
                    WHERE s.[Date] >= @FromUtc
                      AND s.[Date] <  @ToUtc
                    GROUP BY d.ProductId, p.Code, p.Name
                    ORDER BY QuantitySold DESC, Revenue DESC;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_TopProducts;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_LowStock;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_SalesByDay;");
        }
    }
}
