using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FloodAI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "areas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parent_id = table.Column<long>(type: "bigint", nullable: true),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    level = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_areas", x => x.id);
                    table.ForeignKey(
                        name: "FK_areas_areas_parent_id",
                        column: x => x.parent_id,
                        principalTable: "areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "basins",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_basins", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "data_sources",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    url = table.Column<string>(type: "text", nullable: true),
                    source_type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_data_sources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "metrics",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    quantity_type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metrics", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.CheckConstraint("ck_users_normalized_email", "email = lower(btrim(email)) AND email <> ''");
                    table.CheckConstraint("ck_users_role", "role IN ('USER', 'ADMIN')");
                });

            migrationBuilder.CreateTable(
                name: "stations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    basin_id = table.Column<long>(type: "bigint", nullable: false),
                    area_id = table.Column<long>(type: "bigint", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    lat = table.Column<double>(type: "double precision", nullable: true),
                    lon = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stations", x => x.id);
                    table.ForeignKey(
                        name: "FK_stations_areas_area_id",
                        column: x => x.area_id,
                        principalTable: "areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stations_basins_basin_id",
                        column: x => x.basin_id,
                        principalTable: "basins",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_area_follows",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    area_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_area_follows", x => new { x.user_id, x.area_id });
                    table.ForeignKey(
                        name: "FK_user_area_follows_areas_area_id",
                        column: x => x.area_id,
                        principalTable: "areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_area_follows_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "community_posts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    author_id = table.Column<long>(type: "bigint", nullable: false),
                    area_id = table.Column<long>(type: "bigint", nullable: false),
                    station_id = table.Column<long>(type: "bigint", nullable: true),
                    title = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    post_type = table.Column<string>(type: "text", nullable: false),
                    observed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    moderation_status = table.Column<string>(type: "text", nullable: false, defaultValue: "PENDING"),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_community_posts", x => x.id);
                    table.CheckConstraint("ck_community_posts_moderation_status", "moderation_status IN ('PENDING', 'APPROVED', 'REJECTED', 'HIDDEN')");
                    table.ForeignKey(
                        name: "FK_community_posts_areas_area_id",
                        column: x => x.area_id,
                        principalTable: "areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_community_posts_stations_station_id",
                        column: x => x.station_id,
                        principalTable: "stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_community_posts_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "forecast_models",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    station_id = table.Column<long>(type: "bigint", nullable: false),
                    version = table.Column<string>(type: "text", nullable: false),
                    horizon_hours = table.Column<int>(type: "integer", nullable: false),
                    artifact_path = table.Column<string>(type: "text", nullable: false),
                    manifest_hash = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_forecast_models", x => x.id);
                    table.ForeignKey(
                        name: "FK_forecast_models_stations_station_id",
                        column: x => x.station_id,
                        principalTable: "stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "forecast_runs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    station_id = table.Column<long>(type: "bigint", nullable: false),
                    issue_time_local = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    model_bundle_hash = table.Column<string>(type: "text", nullable: false),
                    input_hash = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_forecast_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_forecast_runs_stations_station_id",
                        column: x => x.station_id,
                        principalTable: "stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "observations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    station_id = table.Column<long>(type: "bigint", nullable: false),
                    metric_id = table.Column<long>(type: "bigint", nullable: false),
                    source_id = table.Column<long>(type: "bigint", nullable: false),
                    timestamp_local = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    value_raw = table.Column<double>(type: "double precision", nullable: false),
                    quality_status = table.Column<string>(type: "text", nullable: false),
                    available_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_observations", x => x.id);
                    table.ForeignKey(
                        name: "FK_observations_data_sources_source_id",
                        column: x => x.source_id,
                        principalTable: "data_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_observations_metrics_metric_id",
                        column: x => x.metric_id,
                        principalTable: "metrics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_observations_stations_station_id",
                        column: x => x.station_id,
                        principalTable: "stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "station_thresholds",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    station_id = table.Column<long>(type: "bigint", nullable: false),
                    threshold_type = table.Column<string>(type: "text", nullable: false),
                    value_raw = table.Column<double>(type: "double precision", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    unit_status = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_station_thresholds", x => x.id);
                    table.ForeignKey(
                        name: "FK_station_thresholds_stations_station_id",
                        column: x => x.station_id,
                        principalTable: "stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user_station_follows",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    station_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_station_follows", x => new { x.user_id, x.station_id });
                    table.ForeignKey(
                        name: "FK_user_station_follows_stations_station_id",
                        column: x => x.station_id,
                        principalTable: "stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_station_follows_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "post_comments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    post_id = table.Column<long>(type: "bigint", nullable: false),
                    author_id = table.Column<long>(type: "bigint", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    moderation_status = table.Column<string>(type: "text", nullable: false, defaultValue: "PENDING"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_comments", x => x.id);
                    table.CheckConstraint("ck_post_comments_moderation_status", "moderation_status IN ('PENDING', 'APPROVED', 'REJECTED', 'HIDDEN')");
                    table.ForeignKey(
                        name: "FK_post_comments_community_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "community_posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_post_comments_users_author_id",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "post_media",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    post_id = table.Column<long>(type: "bigint", nullable: false),
                    storage_key = table.Column<string>(type: "text", nullable: false),
                    media_type = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_post_media", x => x.id);
                    table.ForeignKey(
                        name: "FK_post_media_community_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "community_posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "forecast_values",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    run_id = table.Column<long>(type: "bigint", nullable: false),
                    model_id = table.Column<long>(type: "bigint", nullable: false),
                    target_time_local = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    horizon_hours = table.Column<int>(type: "integer", nullable: false),
                    predicted_value_raw = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_forecast_values", x => x.id);
                    table.ForeignKey(
                        name: "FK_forecast_values_forecast_models_model_id",
                        column: x => x.model_id,
                        principalTable: "forecast_models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_forecast_values_forecast_runs_run_id",
                        column: x => x.run_id,
                        principalTable: "forecast_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "content_reports",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    reporter_id = table.Column<long>(type: "bigint", nullable: false),
                    post_id = table.Column<long>(type: "bigint", nullable: true),
                    comment_id = table.Column<long>(type: "bigint", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    resolved_by = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_reports", x => x.id);
                    table.CheckConstraint("ck_content_reports_single_target", "(post_id IS NOT NULL) <> (comment_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_content_reports_community_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "community_posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_content_reports_post_comments_comment_id",
                        column: x => x.comment_id,
                        principalTable: "post_comments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_content_reports_users_reporter_id",
                        column: x => x.reporter_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_content_reports_users_resolved_by",
                        column: x => x.resolved_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "moderation_actions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admin_id = table.Column<long>(type: "bigint", nullable: false),
                    post_id = table.Column<long>(type: "bigint", nullable: true),
                    comment_id = table.Column<long>(type: "bigint", nullable: true),
                    action = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_actions", x => x.id);
                    table.CheckConstraint("ck_moderation_actions_single_target", "(post_id IS NOT NULL) <> (comment_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_moderation_actions_community_posts_post_id",
                        column: x => x.post_id,
                        principalTable: "community_posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_moderation_actions_post_comments_comment_id",
                        column: x => x.comment_id,
                        principalTable: "post_comments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_moderation_actions_users_admin_id",
                        column: x => x.admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "warning_evaluations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    forecast_value_id = table.Column<long>(type: "bigint", nullable: false),
                    threshold_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    evaluated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warning_evaluations", x => x.id);
                    table.ForeignKey(
                        name: "FK_warning_evaluations_forecast_values_forecast_value_id",
                        column: x => x.forecast_value_id,
                        principalTable: "forecast_values",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_warning_evaluations_station_thresholds_threshold_id",
                        column: x => x.threshold_id,
                        principalTable: "station_thresholds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_areas_code",
                table: "areas",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_areas_parent_id",
                table: "areas",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_basins_code",
                table: "basins",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_community_posts_area_id_moderation_status_published_at",
                table: "community_posts",
                columns: new[] { "area_id", "moderation_status", "published_at" });

            migrationBuilder.CreateIndex(
                name: "IX_community_posts_author_id",
                table: "community_posts",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_community_posts_station_id",
                table: "community_posts",
                column: "station_id");

            migrationBuilder.CreateIndex(
                name: "IX_content_reports_comment_id",
                table: "content_reports",
                column: "comment_id");

            migrationBuilder.CreateIndex(
                name: "IX_content_reports_post_id",
                table: "content_reports",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "IX_content_reports_reporter_id",
                table: "content_reports",
                column: "reporter_id");

            migrationBuilder.CreateIndex(
                name: "IX_content_reports_resolved_by",
                table: "content_reports",
                column: "resolved_by");

            migrationBuilder.CreateIndex(
                name: "IX_forecast_models_station_id_version_horizon_hours",
                table: "forecast_models",
                columns: new[] { "station_id", "version", "horizon_hours" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_forecast_runs_station_id",
                table: "forecast_runs",
                column: "station_id");

            migrationBuilder.CreateIndex(
                name: "IX_forecast_values_model_id",
                table: "forecast_values",
                column: "model_id");

            migrationBuilder.CreateIndex(
                name: "IX_forecast_values_run_id_horizon_hours",
                table: "forecast_values",
                columns: new[] { "run_id", "horizon_hours" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_metrics_code",
                table: "metrics",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_moderation_actions_admin_id",
                table: "moderation_actions",
                column: "admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_moderation_actions_comment_id",
                table: "moderation_actions",
                column: "comment_id");

            migrationBuilder.CreateIndex(
                name: "IX_moderation_actions_post_id",
                table: "moderation_actions",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "IX_observations_metric_id",
                table: "observations",
                column: "metric_id");

            migrationBuilder.CreateIndex(
                name: "IX_observations_source_id",
                table: "observations",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "IX_observations_station_id_metric_id_source_id_timestamp_local",
                table: "observations",
                columns: new[] { "station_id", "metric_id", "source_id", "timestamp_local" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_post_comments_author_id",
                table: "post_comments",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_post_comments_post_id_moderation_status_created_at",
                table: "post_comments",
                columns: new[] { "post_id", "moderation_status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_post_media_post_id",
                table: "post_media",
                column: "post_id");

            migrationBuilder.CreateIndex(
                name: "IX_station_thresholds_station_id",
                table: "station_thresholds",
                column: "station_id");

            migrationBuilder.CreateIndex(
                name: "IX_stations_area_id",
                table: "stations",
                column: "area_id");

            migrationBuilder.CreateIndex(
                name: "IX_stations_basin_id_code",
                table: "stations",
                columns: new[] { "basin_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_area_follows_area_id",
                table: "user_area_follows",
                column: "area_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_station_follows_station_id",
                table: "user_station_follows",
                column: "station_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_warning_evaluations_forecast_value_id",
                table: "warning_evaluations",
                column: "forecast_value_id");

            migrationBuilder.CreateIndex(
                name: "IX_warning_evaluations_threshold_id",
                table: "warning_evaluations",
                column: "threshold_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_reports");

            migrationBuilder.DropTable(
                name: "moderation_actions");

            migrationBuilder.DropTable(
                name: "observations");

            migrationBuilder.DropTable(
                name: "post_media");

            migrationBuilder.DropTable(
                name: "user_area_follows");

            migrationBuilder.DropTable(
                name: "user_station_follows");

            migrationBuilder.DropTable(
                name: "warning_evaluations");

            migrationBuilder.DropTable(
                name: "post_comments");

            migrationBuilder.DropTable(
                name: "data_sources");

            migrationBuilder.DropTable(
                name: "metrics");

            migrationBuilder.DropTable(
                name: "forecast_values");

            migrationBuilder.DropTable(
                name: "station_thresholds");

            migrationBuilder.DropTable(
                name: "community_posts");

            migrationBuilder.DropTable(
                name: "forecast_models");

            migrationBuilder.DropTable(
                name: "forecast_runs");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "stations");

            migrationBuilder.DropTable(
                name: "areas");

            migrationBuilder.DropTable(
                name: "basins");
        }
    }
}
