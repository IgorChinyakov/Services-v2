using DirectoryService.Domain.Entities;
using DirectoryService.Domain.Entities.Ids;
using DirectoryService.Domain.ValueObjects.Location;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DirectoryService.Infrastructure.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("locations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => LocationId.Create(value))
            .HasColumnName("id");

        builder.OwnsOne(x => x.Name, nameBuilder =>
        {
            nameBuilder.Property(x => x.Value)
                .HasColumnName("name")
                .HasMaxLength(Name.MAX_LENGTH)
                .IsRequired();
        });

        builder.OwnsOne(x => x.Address, addressBuilder =>
        {
            addressBuilder.Property(x => x.Country)
                .HasColumnName("country")
                .HasMaxLength(Address.MAX_PART_LENGTH)
                .IsRequired();

            addressBuilder.Property(x => x.City)
                .HasColumnName("city")
                .HasMaxLength(Address.MAX_PART_LENGTH)
                .IsRequired();

            addressBuilder.Property(x => x.Street)
                .HasColumnName("street")
                .HasMaxLength(Address.MAX_PART_LENGTH)
                .IsRequired();

            addressBuilder.Property(x => x.Building)
                .HasColumnName("building")
                .HasMaxLength(Address.MAX_PART_LENGTH)
                .IsRequired();
        });

        builder.OwnsOne(x => x.TimeZone, timeZoneBuilder =>
        {
            timeZoneBuilder.Property(x => x.Value)
                .HasColumnName("time_zone")
                .IsRequired();
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

        builder.Navigation(x => x.DepartmentLocations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("ix_location_created_at");

        builder.OwnsOne(x => x.Name)
            .HasIndex(x => x.Value)
            .HasDatabaseName("ix_locations_name");
    }
}