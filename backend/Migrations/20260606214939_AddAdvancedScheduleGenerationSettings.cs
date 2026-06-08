using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedScheduleGenerationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BalanceWeekends",
                table: "ScheduleGenerationSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxConsecutiveWorkDays",
                table: "ScheduleGenerationSettings",
                type: "int",
                nullable: false,
                defaultValue: 5);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BalanceWeekends",
                table: "ScheduleGenerationSettings");

            migrationBuilder.DropColumn(
                name: "MaxConsecutiveWorkDays",
                table: "ScheduleGenerationSettings");
        }
    }
}
