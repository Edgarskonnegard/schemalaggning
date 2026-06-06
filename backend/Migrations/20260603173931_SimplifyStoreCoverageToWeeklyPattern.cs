using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyStoreCoverageToWeeklyPattern : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoreCoverageRules_StoreId_WeekInCycle_DayOfWeek_ShiftTypeId",
                table: "StoreCoverageRules");

            migrationBuilder.DropColumn(
                name: "WeekInCycle",
                table: "StoreCoverageRules");

            migrationBuilder.CreateIndex(
                name: "IX_StoreCoverageRules_StoreId_DayOfWeek_ShiftTypeId",
                table: "StoreCoverageRules",
                columns: new[] { "StoreId", "DayOfWeek", "ShiftTypeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoreCoverageRules_StoreId_DayOfWeek_ShiftTypeId",
                table: "StoreCoverageRules");

            migrationBuilder.AddColumn<int>(
                name: "WeekInCycle",
                table: "StoreCoverageRules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_StoreCoverageRules_StoreId_WeekInCycle_DayOfWeek_ShiftTypeId",
                table: "StoreCoverageRules",
                columns: new[] { "StoreId", "WeekInCycle", "DayOfWeek", "ShiftTypeId" },
                unique: true);
        }
    }
}
