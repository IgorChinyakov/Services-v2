using DirectoryService.Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DirectoryService.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DirectoryServiceDbContext))]
    [Migration("20260616173500_AddDepartmentAndPositionCreationSupport")]
    public partial class AddDepartmentAndPositionCreationSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "depth",
                table: "departments",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<string>(
                name: "path",
                table: "departments",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.Sql("UPDATE departments SET path = lower(identifier) WHERE path = '';");

            migrationBuilder.CreateIndex(
                name: "ix_departments_path",
                table: "departments",
                column: "path",
                unique: true);

            migrationBuilder.DropIndex(
                name: "ix_positions_name",
                table: "positions");

            migrationBuilder.CreateIndex(
                name: "ix_positions_active_name",
                table: "positions",
                column: "name",
                unique: true,
                filter: "is_active = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_departments_path",
                table: "departments");

            migrationBuilder.DropIndex(
                name: "ix_positions_active_name",
                table: "positions");

            migrationBuilder.DropColumn(
                name: "depth",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "path",
                table: "departments");

            migrationBuilder.CreateIndex(
                name: "ix_positions_name",
                table: "positions",
                column: "name");
        }
    }
}
