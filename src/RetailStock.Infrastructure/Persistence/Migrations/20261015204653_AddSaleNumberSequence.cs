using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailStock.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleNumberSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "SaleNumbers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "SaleNumbers");
        }
    }
}
