using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ECommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class add_operation_claims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OperationClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationClaims", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserOperationClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationClaimId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOperationClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserOperationClaims_OperationClaims_OperationClaimId",
                        column: x => x.OperationClaimId,
                        principalTable: "OperationClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserOperationClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "OperationClaims",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("0f88c5f4-b720-4506-b6cc-901df4c9c148"), "Product.Admin" },
                    { new Guid("10a607ca-391b-48c0-adf9-d615d87863f4"), "Product.Update" },
                    { new Guid("21ba4447-0d0d-4242-935d-8087718d36b5"), "OrderItem.Delete" },
                    { new Guid("22bba1ae-46ab-412c-a82a-b3dfa42252c2"), "Product.Write" },
                    { new Guid("24b8bac0-7fc2-4550-994c-f6e22ba7af94"), "OrderItem.Write" },
                    { new Guid("2ac55953-92fc-4ed2-ae6e-2bb5f079fd16"), "Order.Update" },
                    { new Guid("3ec75534-443a-49fd-a426-d9c1ca8e0257"), "Admin" },
                    { new Guid("6e818194-6bc9-4c80-8c14-03219b616261"), "OrderItem.Update" },
                    { new Guid("74095b97-f2e2-4afc-b97a-eac89510afdb"), "Order.Read" },
                    { new Guid("76115df6-7f55-42d5-8c6b-c7b7ea0d6327"), "OrderItem.Admin" },
                    { new Guid("8d26828e-7cc9-4364-b93e-b47038774042"), "OrderItem.Read" },
                    { new Guid("92c2c10a-ad94-41d9-a946-519e00abafbc"), "Product.Delete" },
                    { new Guid("a262973a-f85d-4f28-8086-61e7824812a2"), "Order.Delete" },
                    { new Guid("b3ec174f-eb56-4b27-9cc0-b3e7a0c584ac"), "Order.Admin" },
                    { new Guid("b3fdabda-cb8e-4b08-8cdb-344789bbeb86"), "Order.Write" },
                    { new Guid("cf499e30-e50d-4750-9274-9d376187c422"), "Product.Create" },
                    { new Guid("d0894728-57ef-430c-aa75-395af47a209c"), "Product.Read" },
                    { new Guid("e6a68171-dd72-432a-ad1a-5cf3a40026d1"), "Order.Create" },
                    { new Guid("fcc18799-a665-4f8e-adc2-4ae0f6a08155"), "OrderItem.Create" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperationClaims_Name",
                table: "OperationClaims",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserOperationClaims_OperationClaimId",
                table: "UserOperationClaims",
                column: "OperationClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOperationClaims_UserId_OperationClaimId",
                table: "UserOperationClaims",
                columns: new[] { "UserId", "OperationClaimId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserOperationClaims");

            migrationBuilder.DropTable(
                name: "OperationClaims");
        }
    }
}
