using Microsoft.EntityFrameworkCore.Migrations;

namespace DirectoryService.Infrastructure.Migrations;

public partial class MakeLocationAddressUnique : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "ix_locations_address",
            table: "locations",
            columns:
            [
                "country",
                "city",
                "street",
                "building",
            ],
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_locations_address",
            table: "locations");
    }
}
