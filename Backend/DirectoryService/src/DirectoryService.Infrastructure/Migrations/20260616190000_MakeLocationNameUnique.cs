using DirectoryService.Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DirectoryService.Infrastructure.Migrations;

[DbContext(typeof(DirectoryServiceDbContext))]
[Migration("20260616190000_MakeLocationNameUnique")]
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
