using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT15_MonticalboFinalProj.Migrations
{
    /// <inheritdoc />
    public partial class DefenseScheduleAndDeliverableEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentFileName",
                table: "ProjectTasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentPath",
                table: "ProjectTasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrderIndex",
                table: "ProjectTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "DeliverableSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdviserAvailabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdviserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AvailableDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsBooked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdviserAvailabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdviserAvailabilities_AspNetUsers_AdviserId",
                        column: x => x.AdviserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DefenseSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScheduledById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ApprovedByAdviserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DefenseSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DefenseSchedules_AspNetUsers_ApprovedByAdviserId",
                        column: x => x.ApprovedByAdviserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DefenseSchedules_AspNetUsers_ScheduledById",
                        column: x => x.ScheduledById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DefenseSchedules_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdviserAvailabilities_AdviserId",
                table: "AdviserAvailabilities",
                column: "AdviserId");

            migrationBuilder.CreateIndex(
                name: "IX_DefenseSchedules_ApprovedByAdviserId",
                table: "DefenseSchedules",
                column: "ApprovedByAdviserId");

            migrationBuilder.CreateIndex(
                name: "IX_DefenseSchedules_ProjectId",
                table: "DefenseSchedules",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_DefenseSchedules_ScheduledById",
                table: "DefenseSchedules",
                column: "ScheduledById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdviserAvailabilities");

            migrationBuilder.DropTable(
                name: "DefenseSchedules");

            migrationBuilder.DropColumn(
                name: "AttachmentFileName",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "AttachmentPath",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "OrderIndex",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "DeliverableSubmissions");
        }
    }
}
