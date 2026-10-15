using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailStock.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReportingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sales_Date",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_SaleDetails_SaleId",
                table: "SaleDetails");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_Quantity",
                table: "Stocks",
                column: "Quantity");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_Date",
                table: "Sales",
                column: "Date")
                .Annotation("SqlServer:Include", new[] { "Total" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleDetails_SaleId",
                table: "SaleDetails",
                column: "SaleId")
                .Annotation("SqlServer:Include", new[] { "ProductId", "Quantity", "LineTotal" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stocks_Quantity",
                table: "Stocks");

            migrationBuilder.DropIndex(
                name: "IX_Sales_Date",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_SaleDetails_SaleId",
                table: "SaleDetails");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_Date",
                table: "Sales",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_SaleDetails_SaleId",
                table: "SaleDetails",
                column: "SaleId");
        }
    }
}
