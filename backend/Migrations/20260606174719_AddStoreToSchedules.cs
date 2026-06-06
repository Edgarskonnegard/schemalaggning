using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreToSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StoreId",
                table: "Schedules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM Stores)
                BEGIN
                    INSERT INTO Stores (Name) VALUES ('Default')
                END

                UPDATE Schedules
                SET StoreId = (SELECT TOP(1) Id FROM Stores ORDER BY Id)
                WHERE StoreId = 0
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_StoreId",
                table: "Schedules",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Schedules_Stores_StoreId",
                table: "Schedules",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schedules_Stores_StoreId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_StoreId",
                table: "Schedules");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Schedules");
        }
    }
}
