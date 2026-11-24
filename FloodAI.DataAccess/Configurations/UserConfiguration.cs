using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloodAI.DataAccess.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", table =>
        {
            table.HasCheckConstraint("ck_users_normalized_email", "email = lower(btrim(email)) AND email <> ''");
            table.HasCheckConstraint("ck_users_role", "role IN ('USER', 'ADMIN')");
        });

        builder.Property(user => user.Id).HasColumnName("id");
        builder.Property(user => user.Email).HasColumnName("email");
        builder.Property(user => user.PasswordHash).HasColumnName("password_hash");
        builder.Property(user => user.DisplayName).HasColumnName("display_name");
        builder.Property(user => user.Role).HasColumnName("role");
        builder.Property(user => user.Status).HasColumnName("status");
        builder.Property(user => user.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(user => user.Email).IsUnique();
    }
}
