using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloodAI.DataAccess.Configurations;

internal sealed class PostCommentConfiguration : IEntityTypeConfiguration<PostComment>
{
    public void Configure(EntityTypeBuilder<PostComment> builder)
    {
        builder.ToTable("post_comments", table =>
        {
            table.HasCheckConstraint("ck_post_comments_moderation_status", "moderation_status IN ('PENDING', 'APPROVED', 'REJECTED', 'HIDDEN')");
        });

        builder.Property(comment => comment.Id).HasColumnName("id");
        builder.Property(comment => comment.PostId).HasColumnName("post_id");
        builder.Property(comment => comment.AuthorId).HasColumnName("author_id");
        builder.Property(comment => comment.Content).HasColumnName("content");
        builder.Property(comment => comment.ModerationStatus)
            .HasColumnName("moderation_status")
            .HasDefaultValue("PENDING");
        builder.Property(comment => comment.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(comment => comment.PublishedAt).HasColumnName("published_at");
        builder.Property(comment => comment.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(comment => new { comment.PostId, comment.ModerationStatus, comment.CreatedAt });

        builder.HasOne(comment => comment.Post)
            .WithMany(related => related.Comments)
            .HasForeignKey(comment => comment.PostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(comment => comment.Author)
            .WithMany()
            .HasForeignKey(comment => comment.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
