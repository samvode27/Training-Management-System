using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        // Primary key (convention already sets Id as PK, but explicit is fine)
        builder.HasKey(s => s.Id);

        // Natural key: RegistrationNumber – we'll add unique index in Session 3 or later
        builder.Property(s => s.RegistrationNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.GPA)
            .HasPrecision(3, 2); // e.g., 3.80

        // Relationship: Student has many Enrollments
        builder.HasMany(s => s.Enrollments)
            .WithOne(e => e.Student)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict); // We'll discuss in Exercise 5

        builder.Property<DateTime>("LastUpdated")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(s => s.Version)
            .IsRowVersion();    
            
        builder.HasQueryFilter(s => !s.IsDeleted);
    }
}