using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloodAI.DataAccess.Configurations;

internal sealed class ModerationActionConfiguration : IEntityTypeConfiguration<ModerationAction>
{
    public void Configure(EntityTypeBuilder<ModerationAction> builder)
    {
        builder.ToTable("moderation_actions", table =>
        {
            table.HasCheckConstraint("ck_moderation_actions_single_target", "(post_id IS NOT NULL) <> (comment_id IS NOT NULL)");
        });

        builder.Property(action => action.Id).HasColumnName("id");
        builder.Property(action => action.AdminId).HasColumnName("admin_id");
        builder.Property(action => action.PostId).HasColumnName("post_id");
        builder.Property(action => action.CommentId).HasColumnName("comment_id");
        builder.Property(action => action.Action).HasColumnName("action");
        builder.Property(action => action.Reason).HasColumnName("reason");
        builder.Property(action => action.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(action => action.Admin)
            .WithMany()
            .HasForeignKey(action => action.AdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(action => action.Post)
            .WithMany()
            .HasForeignKey(action => action.PostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(action => action.Comment)
            .WithMany()
            .HasForeignKey(action => action.CommentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
