using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DirectoryService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlignDomainWithDs2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "time_zone",
                table: "locations");

            migrationBuilder.RenameColumn(
                name: "identifier",
                table: "departments",
                newName: "slug");

            migrationBuilder.Sql("UPDATE departments SET slug = lower(slug);");

            migrationBuilder.RenameIndex(
                name: "ix_departments_identifier",
                table: "departments",
                newName: "ix_departments_slug");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "slug",
                table: "departments",
                newName: "identifier");

            migrationBuilder.RenameIndex(
                name: "ix_departments_slug",
                table: "departments",
                newName: "ix_departments_identifier");

            migrationBuilder.AddColumn<string>(
                name: "time_zone",
                table: "locations",
                type: "text",
                nullable: false,
                defaultValue: string.Empty);
        }
    }
}
