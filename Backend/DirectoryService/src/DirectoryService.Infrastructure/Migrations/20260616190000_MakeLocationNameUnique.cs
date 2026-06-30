using Microsoft.EntityFrameworkCore.Migrations;

namespace DirectoryService.Infrastructure.Migrations;

public partial class MakeLocationNameUnique : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_locations_name",
            table: "locations");

        migrationBuilder.CreateIndex(
            name: "ix_locations_name",
            table: "locations",
            column: "name",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_locations_name",
            table: "locations");

        migrationBuilder.CreateIndex(
            name: "ix_locations_name",
            table: "locations",
            column: "name");
    }
}
