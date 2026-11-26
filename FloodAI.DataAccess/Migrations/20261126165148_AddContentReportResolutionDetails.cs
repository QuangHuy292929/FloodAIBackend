using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloodAI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddContentReportResolutionDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "resolution_reason",
                table: "content_reports",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "resolved_at",
                table: "content_reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_content_reports_resolution_details",
                table: "content_reports",
                sql: "(resolved_at IS NULL AND resolution_reason IS NULL) OR (resolved_at IS NOT NULL AND resolution_reason IS NOT NULL AND btrim(resolution_reason) <> '' AND resolved_by IS NOT NULL AND status IN ('RESOLVED', 'DISMISSED'))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_content_reports_resolution_details",
                table: "content_reports");

            migrationBuilder.DropColumn(
                name: "resolution_reason",
                table: "content_reports");

            migrationBuilder.DropColumn(
                name: "resolved_at",
                table: "content_reports");
        }
    }
}
