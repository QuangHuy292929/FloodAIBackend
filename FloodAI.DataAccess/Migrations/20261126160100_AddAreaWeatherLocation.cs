using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FloodAI.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAreaWeatherLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "weather_latitude",
                table: "areas",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "weather_longitude",
                table: "areas",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_areas_weather_coordinates",
                table: "areas",
                sql: "(weather_latitude IS NULL AND weather_longitude IS NULL) OR (weather_latitude IS NOT NULL AND weather_longitude IS NOT NULL AND weather_latitude BETWEEN -90 AND 90 AND weather_longitude BETWEEN -180 AND 180)");

            migrationBuilder.Sql("""
                UPDATE areas
                SET weather_latitude = 20.86472, weather_longitude = 106.68333
                WHERE code = 'hai_phong' AND weather_latitude IS NULL AND weather_longitude IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_areas_weather_coordinates",
                table: "areas");

            migrationBuilder.DropColumn(
                name: "weather_latitude",
                table: "areas");

            migrationBuilder.DropColumn(
                name: "weather_longitude",
                table: "areas");
        }
    }
}
