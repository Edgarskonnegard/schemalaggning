using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddSeasonalEmployeesAndCoveragePeriods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoreCoverageRules_StoreId_DayOfWeek_ShiftTypeId",
                table: "StoreCoverageRules");

            migrationBuilder.AddColumn<DateOnly>(
                name: "EffectiveFrom",
                table: "StoreCoverageRules",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EffectiveTo",
                table: "StoreCoverageRules",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "AvailableFrom",
                table: "Employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "AvailableTo",
                table: "Employees",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreCoverageRules_StoreId_DayOfWeek_ShiftTypeId_EffectiveFrom_EffectiveTo",
                table: "StoreCoverageRules",
                columns: new[] { "StoreId", "DayOfWeek", "ShiftTypeId", "EffectiveFrom", "EffectiveTo" },
                unique: true,
                filter: "[EffectiveFrom] IS NOT NULL AND [EffectiveTo] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoreCoverageRules_StoreId_DayOfWeek_ShiftTypeId_EffectiveFrom_EffectiveTo",
                table: "StoreCoverageRules");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                table: "StoreCoverageRules");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                table: "StoreCoverageRules");

            migrationBuilder.DropColumn(
                name: "AvailableFrom",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "AvailableTo",
                table: "Employees");

            migrationBuilder.CreateIndex(
                name: "IX_StoreCoverageRules_StoreId_DayOfWeek_ShiftTypeId",
                table: "StoreCoverageRules",
                columns: new[] { "StoreId", "DayOfWeek", "ShiftTypeId" },
                unique: true);
        }
    }
}
