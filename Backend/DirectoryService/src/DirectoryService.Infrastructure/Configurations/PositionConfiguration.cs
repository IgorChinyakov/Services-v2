using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.ValueObjects.Position;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DirectoryService.Infrastructure.Configurations;

public class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("positions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => PositionId.Create(value))
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.OwnsOne(x => x.Name, nameBuilder =>
        {
            nameBuilder.Property(x => x.Value)
                .HasColumnName("name")
                .HasMaxLength(Name.MAX_LENGTH)
                .IsRequired();
        });

        builder.OwnsOne(x => x.Description, descriptionBuilder =>
        {
            descriptionBuilder.Property(x => x.Value)
                .HasColumnName("description")
                .HasMaxLength(Description.MAX_LENGTH)
                .IsRequired(false);
        });

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Navigation(x => x.DepartmentPositions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("ix_position_created_at");

        builder.OwnsOne(x => x.Name)
            .HasIndex(x => x.Value)
            .HasDatabaseName("ix_positions_name");
    }
}