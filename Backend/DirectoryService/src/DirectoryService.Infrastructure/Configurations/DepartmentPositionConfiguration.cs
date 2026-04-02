using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Entities.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DirectoryService.Infrastructure.Configurations;

public class DepartmentPositionConfiguration : IEntityTypeConfiguration<DepartmentPosition>
{
    public void Configure(EntityTypeBuilder<DepartmentPosition> builder)
    {
        builder.ToTable("department_positions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.DepartmentId)
            .HasColumnName("department_id")
            .HasConversion(
                id => id.Value,
                value => DepartmentId.Create(value))
            .IsRequired();

        builder.Property(x => x.PositionId)
            .HasColumnName("position_id")
            .HasConversion(
                id => id.Value,
                value => PositionId.Create(value))
            .IsRequired();

        builder.HasOne(x => x.Department)
            .WithMany(x => x.DepartmentPositions)
            .HasForeignKey(x => x.DepartmentId)
            .HasConstraintName("fk_department_positions_department_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Position)
            .WithMany(x => x.DepartmentPositions)
            .HasForeignKey(x => x.PositionId)
            .HasConstraintName("fk_department_positions_position_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.DepartmentId).HasDatabaseName("idx_department_positions_department_id");
        builder.HasIndex(x => x.PositionId).HasDatabaseName("idx_department_positions_position_id");

        builder.HasIndex(x => new { x.DepartmentId, x.PositionId })
            .IsUnique();
    }
}