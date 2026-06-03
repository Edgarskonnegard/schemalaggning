using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleShiftTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoleShiftTypes",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ShiftTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleShiftTypes", x => new { x.RoleId, x.ShiftTypeId });
                    table.ForeignKey(
                        name: "FK_RoleShiftTypes_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleShiftTypes_ShiftTypes_ShiftTypeId",
                        column: x => x.ShiftTypeId,
                        principalTable: "ShiftTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleShiftTypes_ShiftTypeId",
                table: "RoleShiftTypes",
                column: "ShiftTypeId");

            migrationBuilder.Sql("""
                INSERT INTO RoleShiftTypes (RoleId, ShiftTypeId)
                SELECT RoleId, Id
                FROM ShiftTypes
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_ShiftTypes_Roles_RoleId",
                table: "ShiftTypes");

            migrationBuilder.DropIndex(
                name: "IX_ShiftTypes_RoleId",
                table: "ShiftTypes");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "ShiftTypes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "ShiftTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE ShiftTypes
                SET RoleId = (
                    SELECT TOP 1 RoleShiftTypes.RoleId
                    FROM RoleShiftTypes
                    WHERE RoleShiftTypes.ShiftTypeId = ShiftTypes.Id
                    ORDER BY RoleShiftTypes.RoleId
                )
                """);

            migrationBuilder.DropTable(
                name: "RoleShiftTypes");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftTypes_RoleId",
                table: "ShiftTypes",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftTypes_Roles_RoleId",
                table: "ShiftTypes",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
