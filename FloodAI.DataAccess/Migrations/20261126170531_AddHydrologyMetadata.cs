using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FloodAI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddHydrologyMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "river_branch_id",
                table: "stations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "update_source_id",
                table: "stations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "data_origin",
                table: "observations",
                type: "text",
                nullable: false,
                defaultValue: "UNCLASSIFIED");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ingested_at",
                table: "observations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "unit",
                table: "observations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "supports_updates",
                table: "data_sources",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "time_basis",
                table: "data_sources",
                type: "text",
                nullable: false,
                defaultValue: "SOURCE_LOCAL_UNVERIFIED");

            migrationBuilder.CreateTable(
                name: "river_branches",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    basin_id = table.Column<long>(type: "bigint", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_river_branches", x => x.id);
                    table.UniqueConstraint("AK_river_branches_basin_id_id", x => new { x.basin_id, x.id });
                    table.ForeignKey(
                        name: "FK_river_branches_basins_basin_id",
                        column: x => x.basin_id,
                        principalTable: "basins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stations_basin_id_river_branch_id",
                table: "stations",
                columns: new[] { "basin_id", "river_branch_id" });

            migrationBuilder.CreateIndex(
                name: "IX_stations_update_source_id",
                table: "stations",
                column: "update_source_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_observations_data_origin",
                table: "observations",
                sql: "data_origin IN ('OBSERVED', 'ESTIMATED', 'SYNTHETIC', 'UNCLASSIFIED')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_data_sources_time_basis",
                table: "data_sources",
                sql: "time_basis IN ('SOURCE_LOCAL_UNVERIFIED', 'VIETNAM_LOCAL', 'UTC')");

            migrationBuilder.CreateIndex(
                name: "IX_river_branches_basin_id_code",
                table: "river_branches",
                columns: new[] { "basin_id", "code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_stations_data_sources_update_source_id",
                table: "stations",
                column: "update_source_id",
                principalTable: "data_sources",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_stations_river_branches_basin_id_river_branch_id",
                table: "stations",
                columns: new[] { "basin_id", "river_branch_id" },
                principalTable: "river_branches",
                principalColumns: new[] { "basin_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                UPDATE observations AS o
                SET data_origin = 'OBSERVED'
                FROM data_sources AS d, stations AS s, basins AS b, metrics AS m
                WHERE o.source_id = d.id AND o.station_id = s.id AND s.basin_id = b.id AND o.metric_id = m.id
                  AND d.source_type = 'HISTORICAL_CSV' AND d.name = U&'V\0103n \00DAc 2021\20132022'
                  AND b.code = 'van_uc' AND m.code IN ('water_level', 'discharge')
                  AND o.data_origin = 'UNCLASSIFIED';
                """);

            migrationBuilder.Sql("ALTER TABLE river_branches ENABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stations_data_sources_update_source_id",
                table: "stations");

            migrationBuilder.DropForeignKey(
                name: "FK_stations_river_branches_basin_id_river_branch_id",
                table: "stations");

            migrationBuilder.DropTable(
                name: "river_branches");

            migrationBuilder.DropIndex(
                name: "IX_stations_basin_id_river_branch_id",
                table: "stations");

            migrationBuilder.DropIndex(
                name: "IX_stations_update_source_id",
                table: "stations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_observations_data_origin",
                table: "observations");

            migrationBuilder.DropCheckConstraint(
                name: "ck_data_sources_time_basis",
                table: "data_sources");

            migrationBuilder.DropColumn(
                name: "river_branch_id",
                table: "stations");

            migrationBuilder.DropColumn(
                name: "update_source_id",
                table: "stations");

            migrationBuilder.DropColumn(
                name: "data_origin",
                table: "observations");

            migrationBuilder.DropColumn(
                name: "ingested_at",
                table: "observations");

            migrationBuilder.DropColumn(
                name: "unit",
                table: "observations");

            migrationBuilder.DropColumn(
                name: "supports_updates",
                table: "data_sources");

            migrationBuilder.DropColumn(
                name: "time_basis",
                table: "data_sources");
        }
    }
}
