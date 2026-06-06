using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddUnassignedBaseScheduleDraftRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BaseScheduleUnassignedDraftRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<int>(type: "int", nullable: false),
                    ShiftTypeId = table.Column<int>(type: "int", nullable: false),
                    WeekInCycle = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseScheduleUnassignedDraftRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaseScheduleUnassignedDraftRules_BaseScheduleGenerationBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "BaseScheduleGenerationBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BaseScheduleUnassignedDraftRules_ShiftTypes_ShiftTypeId",
                        column: x => x.ShiftTypeId,
                        principalTable: "ShiftTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BaseScheduleUnassignedDraftRules_BatchId",
                table: "BaseScheduleUnassignedDraftRules",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseScheduleUnassignedDraftRules_ShiftTypeId",
                table: "BaseScheduleUnassignedDraftRules",
                column: "ShiftTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BaseScheduleUnassignedDraftRules");
        }
    }
}
