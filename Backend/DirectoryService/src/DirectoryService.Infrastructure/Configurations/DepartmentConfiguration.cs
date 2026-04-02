using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.ValueObjects.Department;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DirectoryService.Infrastructure.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => DepartmentId.Create(value))
            .HasColumnName("id");

        builder.OwnsOne(x => x.Name, nameBuilder =>
        {
            nameBuilder.Property(x => x.Value)
                .HasColumnName("name")
                .HasMaxLength(Name.MAX_LENGTH)
                .IsRequired();
        });

        builder.OwnsOne(x => x.Identifier, identifierBuilder =>
        {
            identifierBuilder.Property(x => x.Value)
                .HasColumnName("identifier")
                .HasMaxLength(Identifier.MAX_LENGTH)
                .IsRequired();
        });

        builder.Property(x => x.ParentId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value == null ? null : DepartmentId.Create(value.Value))
            .IsRequired(false)
            .HasColumnName("parent_id");

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(x => x.Children)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.DepartmentPositions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(x => x.DepartmentLocations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.ParentId).HasDatabaseName("ix_department_parent_id");
        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("ix_department_created_at");

        builder.OwnsOne(x => x.Name).HasIndex(x => x.Value).HasDatabaseName("ix_departments_name");
        builder.OwnsOne(x => x.Identifier).HasIndex(x => x.Value).IsUnique().HasDatabaseName("ix_departments_identifier");

        builder.Ignore(x => x.Depth);
        builder.Ignore(x => x.Path);
    }
}