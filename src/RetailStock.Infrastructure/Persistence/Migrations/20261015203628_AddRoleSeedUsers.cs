using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RetailStock.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleSeedUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "AQAAAAIAAYagAAAAEGvOOr8DSNVmYjdTL+xQvqbUCyQBkIF/ErRF+y7jIxX28ZYgaegGr3hYj/vpn6WGJA==");

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "PasswordHash", "Role" },
                values: new object[,]
                {
                    { 2, "bodega@retailstock.com", "AQAAAAIAAYagAAAAELbMoKWVpVn1S5usBUaqzR3GapPU3y+RMsJyFCuJijhvE7YA9ueA8Jex/9HPe3Iimg==", "Warehouse" },
                    { 3, "caja@retailstock.com", "AQAAAAIAAYagAAAAEM7ZTdhpJzkQ7PjfpkzMJSlAWzqzTDCaaYQcVl2VjRG2xfeSDJGZlihTC4IhtlA1Mw==", "Cashier" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$2VijrJstQcT869rj49LnmO8nYPgmIDxJtlATEhe8rNrqAWhAIChtW");
        }
    }
}
