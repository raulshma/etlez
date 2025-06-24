using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ETLFramework.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pipelines",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SourceConnectorJson = table.Column<string>(type: "jsonb", nullable: false),
                    TargetConnectorJson = table.Column<string>(type: "jsonb", nullable: false),
                    TransformationsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    ConfigurationJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastExecutedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pipelines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Executions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExecutionId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PipelineId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RecordsProcessed = table.Column<long>(type: "bigint", nullable: false),
                    SuccessfulRecords = table.Column<long>(type: "bigint", nullable: false),
                    FailedRecords = table.Column<long>(type: "bigint", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ParametersJson = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Executions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Executions_Pipelines_PipelineId",
                        column: x => x.PipelineId,
                        principalTable: "Pipelines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Executions_ExecutionId",
                table: "Executions",
                column: "ExecutionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Executions_PipelineId",
                table: "Executions",
                column: "PipelineId");

            migrationBuilder.CreateIndex(
                name: "IX_Executions_PipelineId_StartTime",
                table: "Executions",
                columns: new[] { "PipelineId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Executions_StartTime",
                table: "Executions",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_Executions_Status",
                table: "Executions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Pipelines_CreatedAt",
                table: "Pipelines",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Pipelines_IsEnabled",
                table: "Pipelines",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_Pipelines_LastExecutedAt",
                table: "Pipelines",
                column: "LastExecutedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Pipelines_Name",
                table: "Pipelines",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Executions");

            migrationBuilder.DropTable(
                name: "Pipelines");
        }
    }
}
