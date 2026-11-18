using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class add_product_seed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Name", "Stock", "Price_Amount", "Price_Currency" },
                values: new object[] { new Guid("2dcb27b9-06cf-4053-a423-c2e94ed048a6"), "Deneme ürün", 100, 500m, "TRY" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("2dcb27b9-06cf-4053-a423-c2e94ed048a6"));
        }
    }
}
