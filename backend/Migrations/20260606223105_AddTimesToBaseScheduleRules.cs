using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddTimesToBaseScheduleRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "EndTime",
                table: "BaseScheduleUnassignedDraftRules",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTime",
                table: "BaseScheduleUnassignedDraftRules",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "EndTime",
                table: "BaseScheduleRules",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTime",
                table: "BaseScheduleRules",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "EndTime",
                table: "BaseScheduleDraftRules",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTime",
                table: "BaseScheduleDraftRules",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.Sql("""
                UPDATE rules
                SET
                    rules.StartTime = shiftTypes.DefaultStartTime,
                    rules.EndTime = shiftTypes.DefaultEndTime
                FROM BaseScheduleRules rules
                INNER JOIN ShiftTypes shiftTypes ON shiftTypes.Id = rules.ShiftTypeId;
                """);

            migrationBuilder.Sql("""
                UPDATE rules
                SET
                    rules.StartTime = shiftTypes.DefaultStartTime,
                    rules.EndTime = shiftTypes.DefaultEndTime
                FROM BaseScheduleDraftRules rules
                INNER JOIN ShiftTypes shiftTypes ON shiftTypes.Id = rules.ShiftTypeId;
                """);

            migrationBuilder.Sql("""
                UPDATE rules
                SET
                    rules.StartTime = shiftTypes.DefaultStartTime,
                    rules.EndTime = shiftTypes.DefaultEndTime
                FROM BaseScheduleUnassignedDraftRules rules
                INNER JOIN ShiftTypes shiftTypes ON shiftTypes.Id = rules.ShiftTypeId;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "BaseScheduleUnassignedDraftRules");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "BaseScheduleUnassignedDraftRules");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "BaseScheduleRules");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "BaseScheduleRules");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "BaseScheduleDraftRules");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "BaseScheduleDraftRules");
        }
    }
}
