using Microsoft.EntityFrameworkCore.Migrations;

namespace DirectoryService.Infrastructure.Migrations;

public partial class PrepareDepartmentPathLtree : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS ltree;");

        migrationBuilder.DropIndex(
            name: "ix_departments_path",
            table: "departments");

        migrationBuilder.Sql("ALTER TABLE departments ALTER COLUMN path DROP DEFAULT;");
        migrationBuilder.Sql("ALTER TABLE departments ALTER COLUMN path TYPE ltree USING path::ltree;");

        migrationBuilder.CreateIndex(
            name: "ix_departments_path",
            table: "departments",
            column: "path",
            unique: true);

        migrationBuilder.Sql("CREATE INDEX ix_departments_path_gist ON departments USING GIST (path);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_departments_path",
            table: "departments");

        migrationBuilder.Sql("DROP INDEX IF EXISTS ix_departments_path_gist;");
        migrationBuilder.Sql("ALTER TABLE departments ALTER COLUMN path TYPE character varying(1024) USING path::text;");
        migrationBuilder.Sql("ALTER TABLE departments ALTER COLUMN path SET DEFAULT '';");

        migrationBuilder.CreateIndex(
            name: "ix_departments_path",
            table: "departments",
            column: "path",
            unique: true);
    }
}
