using FloodAI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FloodAI.DataAccess.Configurations;

internal sealed class CommunityPostConfiguration : IEntityTypeConfiguration<CommunityPost>
{
    public void Configure(EntityTypeBuilder<CommunityPost> builder)
    {
        builder.ToTable("community_posts", table =>
        {
            table.HasCheckConstraint("ck_community_posts_moderation_status", "moderation_status IN ('PENDING', 'APPROVED', 'REJECTED', 'HIDDEN')");
        });

        builder.Property(post => post.Id).HasColumnName("id");
        builder.Property(post => post.AuthorId).HasColumnName("author_id");
        builder.Property(post => post.AreaId).HasColumnName("area_id");
        builder.Property(post => post.StationId).HasColumnName("station_id");
        builder.Property(post => post.Title).HasColumnName("title");
        builder.Property(post => post.Content).HasColumnName("content");
        builder.Property(post => post.PostType).HasColumnName("post_type");
        builder.Property(post => post.ObservedAt).HasColumnName("observed_at");
        builder.Property(post => post.ModerationStatus)
            .HasColumnName("moderation_status")
            .HasDefaultValue("PENDING");
        builder.Property(post => post.PublishedAt).HasColumnName("published_at");
        builder.Property(post => post.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(post => new { post.AreaId, post.ModerationStatus, post.PublishedAt });

        builder.HasOne(post => post.Author)
            .WithMany()
            .HasForeignKey(post => post.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(post => post.Area)
            .WithMany()
            .HasForeignKey(post => post.AreaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(post => post.Station)
            .WithMany()
            .HasForeignKey(post => post.StationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
