using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddBaseScheduleApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BaseScheduleGenerationBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StoreId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WarningText = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseScheduleGenerationBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaseScheduleGenerationBatches_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BaseScheduleDraftRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ShiftTypeId = table.Column<int>(type: "int", nullable: false),
                    WeekInCycle = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseScheduleDraftRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaseScheduleDraftRules_BaseScheduleGenerationBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "BaseScheduleGenerationBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BaseScheduleDraftRules_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BaseScheduleDraftRules_ShiftTypes_ShiftTypeId",
                        column: x => x.ShiftTypeId,
                        principalTable: "ShiftTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BaseScheduleDraftRules_BatchId",
                table: "BaseScheduleDraftRules",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseScheduleDraftRules_EmployeeId",
                table: "BaseScheduleDraftRules",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseScheduleDraftRules_ShiftTypeId",
                table: "BaseScheduleDraftRules",
                column: "ShiftTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_BaseScheduleGenerationBatches_StoreId",
                table: "BaseScheduleGenerationBatches",
                column: "StoreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BaseScheduleDraftRules");

            migrationBuilder.DropTable(
                name: "BaseScheduleGenerationBatches");
        }
    }
}
