using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloodAI.DataAccess.Configurations;

internal sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions");

        builder.Property(session => session.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(session => session.UserId).HasColumnName("user_id");
        builder.Property(session => session.TokenHash).HasColumnName("token_hash").HasMaxLength(64);
        builder.Property(session => session.ExpiresAt).HasColumnName("expires_at");
        builder.HasIndex(session => session.TokenHash).IsUnique();

        builder.HasOne(session => session.User)
            .WithMany()
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
