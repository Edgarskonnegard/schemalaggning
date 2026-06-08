using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeBaseScheduleApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BaseScheduleEmployeeApprovals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseScheduleEmployeeApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaseScheduleEmployeeApprovals_BaseScheduleGenerationBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "BaseScheduleGenerationBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BaseScheduleEmployeeApprovals_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BaseScheduleEmployeeApprovals_BatchId_EmployeeId",
                table: "BaseScheduleEmployeeApprovals",
                columns: new[] { "BatchId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BaseScheduleEmployeeApprovals_EmployeeId",
                table: "BaseScheduleEmployeeApprovals",
                column: "EmployeeId");

            migrationBuilder.Sql("""
                INSERT INTO BaseScheduleEmployeeApprovals (BatchId, EmployeeId, Status, ApprovedAt, RejectedAt)
                SELECT DISTINCT BatchId, EmployeeId, 'Pending', NULL, NULL
                FROM BaseScheduleDraftRules
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BaseScheduleEmployeeApprovals");
        }
    }
}
