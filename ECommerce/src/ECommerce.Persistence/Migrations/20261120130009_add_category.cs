using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class add_category : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Products",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "SQL_Latin1_General_CP1_CI_AS")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "OperationClaims",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("3b9a774d-3d8b-419e-adef-2a0d3909875b"), "Category.Read" },
                    { new Guid("646cc778-65fe-4f48-baf9-eafd50912044"), "Category.Create" },
                    { new Guid("65f7e5d9-221d-416a-b447-165ee25e0174"), "Category.Admin" },
                    { new Guid("8d887ad1-4f78-45f7-af05-39528dd5f057"), "Category.Write" },
                    { new Guid("a6d3bc82-3a7e-4bb1-93b8-7b6eb8254754"), "Category.Update" },
                    { new Guid("cd6c5aba-a7d4-42be-b9cb-f3f962d5d723"), "Category.Delete" }
                });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("2dcb27b9-06cf-4053-a423-c2e94ed048a6"),
                column: "CategoryId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Products_CategoryId",
                table: "Products");

            migrationBuilder.DeleteData(
                table: "OperationClaims",
                keyColumn: "Id",
                keyValue: new Guid("3b9a774d-3d8b-419e-adef-2a0d3909875b"));

            migrationBuilder.DeleteData(
                table: "OperationClaims",
                keyColumn: "Id",
                keyValue: new Guid("646cc778-65fe-4f48-baf9-eafd50912044"));

            migrationBuilder.DeleteData(
                table: "OperationClaims",
                keyColumn: "Id",
                keyValue: new Guid("65f7e5d9-221d-416a-b447-165ee25e0174"));

            migrationBuilder.DeleteData(
                table: "OperationClaims",
                keyColumn: "Id",
                keyValue: new Guid("8d887ad1-4f78-45f7-af05-39528dd5f057"));

            migrationBuilder.DeleteData(
                table: "OperationClaims",
                keyColumn: "Id",
                keyValue: new Guid("a6d3bc82-3a7e-4bb1-93b8-7b6eb8254754"));

            migrationBuilder.DeleteData(
                table: "OperationClaims",
                keyColumn: "Id",
                keyValue: new Guid("cd6c5aba-a7d4-42be-b9cb-f3f962d5d723"));

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Products");
        }
    }
}
