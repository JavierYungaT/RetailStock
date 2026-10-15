/* =====================================================================================
   RetailStock - Demo de rendimiento: sargabilidad, Index Seek vs Scan y Key Lookup
   -------------------------------------------------------------------------------------
   Inserta 100.000 ventas de prueba DENTRO de una transacción, compara consultas y al final
   hace ROLLBACK: la base de datos queda exactamente como estaba.

   Ejecutar (desde la raíz del repo):
     docker exec -i retailstock-sqlserver /opt/mssql-tools18/bin/sqlcmd \
       -S localhost -U sa -P 'RetailStock_Dev123!' -C -d RetailStockDb -W \
       < database/scripts/performance-demo.sql

   O abrirlo en VS Code con la extensión "SQL Server (mssql)" y activar el plan de ejecución real.

   Qué mirar:
     - "Scan count / logical reads" (SET STATISTICS IO): páginas de 8 KB leídas. Menos = mejor.
     - Operadores del plan (SET STATISTICS PROFILE, columna StmtText):
         Index Seek   -> navega el árbol B directo al rango pedido (bueno).
         Index Scan   -> recorre TODO el índice (malo si solo necesitas una parte).
         Key Lookup   -> por cada fila encontrada en el índice no clustered, va a la tabla
                         (índice clustered) a buscar columnas que faltan. Caro con muchas filas.
   ===================================================================================== */
SET NOCOUNT ON;
-- sqlcmd arranca con QUOTED_IDENTIFIER OFF y SQL Server lo exige ON para modificar tablas con
-- índices filtrados (Sales tiene IX_Sales_PendingSap). SSMS/VS Code y SqlClient ya lo traen en ON.
SET QUOTED_IDENTIFIER ON;
BEGIN TRANSACTION;

-- 1) Datos de prueba: 100.000 ventas repartidas a lo largo de 2025 (una cada 5 minutos)
WITH n AS (
    SELECT TOP (100000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS i
    FROM sys.all_objects AS a CROSS JOIN sys.all_objects AS b
)
INSERT INTO dbo.Sales (Number, [Date], CustomerName, CustomerId, Subtotal, Tax, Total, WarehouseId, UserId, SyncedToSap)
SELECT CONCAT('DEMO-', i),
       DATEADD(MINUTE, i * 5, CAST('2025-01-01' AS DATETIME2)),
       N'Cliente demo', '9999999999999',
       10.00, 1.50, 11.50 + (i % 50),
       1 + (i % 2), 3, 0
FROM n;

UPDATE STATISTICS dbo.Sales WITH FULLSCAN;

SET STATISTICS IO ON;
SET STATISTICS PROFILE ON;

/* -------------------------------------------------------------------------------------
   A) MAL ESCRITA: función sobre la columna en el WHERE.
   YEAR([Date]) y MONTH([Date]) se deben calcular para CADA fila antes de comparar, así que
   SQL Server no puede usar el orden del índice IX_Sales_Date para saltar al rango: hace un
   Index Scan de las 100.000 filas. Se dice que el predicado "no es SARGable"
   (Search ARGument able). Lo mismo pasa con CAST([Date] AS DATE) = @dia, CONVERT, ISNULL(col,..),
   col + 1 = @x, o LIKE '%texto'.
   ------------------------------------------------------------------------------------- */
PRINT '=== A) MAL: WHERE YEAR(Date) = 2025 AND MONTH(Date) = 3';
SELECT COUNT(*) AS SalesCount, SUM(Total) AS Total
FROM dbo.Sales
WHERE YEAR([Date]) = 2025 AND MONTH([Date]) = 3;

/* -------------------------------------------------------------------------------------
   B) OPTIMIZADA: rango semiabierto sobre la columna "desnuda".
   [inicio, fin) -> Index Seek sobre IX_Sales_Date: va directo al 1 de marzo y lee solo hasta el
   31 de marzo. Como el índice tiene INCLUDE (Total), SUM(Total) sale del propio índice.
   Se usa "< 2025-04-01" (no "<= 2025-03-31 23:59:59") para no perder fracciones de segundo.
   ------------------------------------------------------------------------------------- */
PRINT '=== B) BIEN: WHERE Date >= 2025-03-01 AND Date < 2025-04-01';
SELECT COUNT(*) AS SalesCount, SUM(Total) AS Total
FROM dbo.Sales
WHERE [Date] >= '2025-03-01' AND [Date] < '2025-04-01';

/* -------------------------------------------------------------------------------------
   C) KEY LOOKUP: se pide Subtotal, que NO está en IX_Sales_Date.
   El Seek encuentra las filas en el índice, pero por cada una hace un Key Lookup al índice
   clustered (PK_Sales) para traer Subtotal. Con pocas filas es aceptable; con miles,
   SQL Server prefiere un Clustered Index Scan. Solución: agregar la columna al INCLUDE
   (como hicimos con Total) o no seleccionar columnas innecesarias (evitar SELECT *).
   ------------------------------------------------------------------------------------- */
PRINT '=== C) KEY LOOKUP: SELECT Date, Subtotal (Subtotal no esta en el indice)';
SELECT [Date], Subtotal
FROM dbo.Sales
WHERE [Date] >= '2025-03-01' AND [Date] < '2025-03-02';

/* -------------------------------------------------------------------------------------
   D) El procedimiento almacenado: debe mostrar Index Seek sobre IX_Sales_Date y ningún Key Lookup.
   ------------------------------------------------------------------------------------- */
PRINT '=== D) EXEC sp_SalesByDay (marzo 2025, hora de Ecuador)';
EXEC dbo.sp_SalesByDay @From = '2025-03-01', @To = '2025-03-03', @UtcOffsetMinutes = -300;

SET STATISTICS PROFILE OFF;
SET STATISTICS IO OFF;

-- La BD queda intacta
ROLLBACK TRANSACTION;
PRINT '=== ROLLBACK: datos de prueba eliminados';
