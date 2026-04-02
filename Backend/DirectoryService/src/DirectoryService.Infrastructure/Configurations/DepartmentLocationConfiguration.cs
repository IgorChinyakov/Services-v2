using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Entities.Ids;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DirectoryService.Infrastructure.Configurations;

public class DepartmentLocationConfiguration : IEntityTypeConfiguration<DepartmentLocation>
{
    public void Configure(EntityTypeBuilder<DepartmentLocation> builder)
    {
        builder.ToTable("department_locations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.DepartmentId)
            .HasColumnName("department_id")
            .HasConversion(
                id => id.Value,
                value => DepartmentId.Create(value))
            .IsRequired();

        builder.Property(x => x.LocationId)
            .HasColumnName("location_id")
            .HasConversion(
                id => id.Value,
                value => LocationId.Create(value))
            .IsRequired();

        builder.HasOne(x => x.Department)
            .WithMany(x => x.DepartmentLocations)
            .HasForeignKey(x => x.DepartmentId)
            .HasConstraintName("fk_department_locations_department_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Location)
            .WithMany(x => x.DepartmentLocations)
            .HasForeignKey(x => x.LocationId)
            .HasConstraintName("fk_department_locations_location_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.DepartmentId).HasDatabaseName("idx_department_locations_department_id");
        builder.HasIndex(x => x.LocationId).HasDatabaseName("idx_department_locations_location_id");

        builder.HasIndex(x => new { x.DepartmentId, x.LocationId })
            .IsUnique();
    }
}