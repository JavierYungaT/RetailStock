using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailStock.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleSapDocEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SapDocEntry",
                table: "Sales",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_PendingSap",
                table: "Sales",
                column: "SyncedToSap",
                filter: "[SyncedToSap] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sales_PendingSap",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "SapDocEntry",
                table: "Sales");
        }
    }
}
