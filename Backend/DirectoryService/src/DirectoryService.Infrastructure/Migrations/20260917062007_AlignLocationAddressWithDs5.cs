using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DirectoryService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignLocationAddressWithDs5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_locations_address",
                table: "locations");

            migrationBuilder.RenameColumn(
                name: "building",
                table: "locations",
                newName: "house");

            migrationBuilder.AddColumn<string>(
                name: "apartment",
                table: "locations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE locations SET apartment = 'legacy-' || id::text WHERE apartment IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "apartment",
                table: "locations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "country",
                table: "locations");

            migrationBuilder.CreateIndex(
                name: "ix_locations_address",
                table: "locations",
                columns: new[] { "city", "street", "house", "apartment" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_locations_address",
                table: "locations");

            migrationBuilder.AddColumn<string>(
                name: "country",
                table: "locations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE locations SET country = 'legacy-' || id::text WHERE country IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "country",
                table: "locations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "apartment",
                table: "locations");

            migrationBuilder.RenameColumn(
                name: "house",
                table: "locations",
                newName: "building");

            migrationBuilder.CreateIndex(
                name: "ix_locations_address",
                table: "locations",
                columns: new[] { "country", "city", "street", "building" },
                unique: true);
        }
    }
}
