using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyEmployeeShiftEligibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeShiftTypes");

            migrationBuilder.DropIndex(
                name: "IX_BaseScheduleRules_EmployeeId_DayOfWeek",
                table: "BaseScheduleRules");

            migrationBuilder.AddColumn<int>(
                name: "WeekInCycle",
                table: "BaseScheduleRules",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_BaseScheduleRules_EmployeeId_WeekInCycle_DayOfWeek",
                table: "BaseScheduleRules",
                columns: new[] { "EmployeeId", "WeekInCycle", "DayOfWeek" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BaseScheduleRules_EmployeeId_WeekInCycle_DayOfWeek",
                table: "BaseScheduleRules");

            migrationBuilder.DropColumn(
                name: "WeekInCycle",
                table: "BaseScheduleRules");

            migrationBuilder.CreateTable(
                name: "EmployeeShiftTypes",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ShiftTypeId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeShiftTypes", x => new { x.EmployeeId, x.ShiftTypeId });
                    table.ForeignKey(
                        name: "FK_EmployeeShiftTypes_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeShiftTypes_ShiftTypes_ShiftTypeId",
                        column: x => x.ShiftTypeId,
                        principalTable: "ShiftTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BaseScheduleRules_EmployeeId_DayOfWeek",
                table: "BaseScheduleRules",
                columns: new[] { "EmployeeId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeShiftTypes_ShiftTypeId",
                table: "EmployeeShiftTypes",
                column: "ShiftTypeId");
        }
    }
}
